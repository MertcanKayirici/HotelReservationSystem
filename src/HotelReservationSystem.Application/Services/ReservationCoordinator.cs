using HotelReservationSystem.Application.Common;
using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Domain.Entities;
using System.Text.Json;

namespace HotelReservationSystem.Application.Services
{
    public class ReservationCoordinator : IReservationCoordinator
    {
        private readonly IIdempotencyService _idempotency;
        private readonly IOutboxService _outbox;
        private readonly IRedisLockService _lock;
        private readonly IReservationRepository _reservationRepository;
        private readonly IUnitOfWork _uow;
        private readonly IAppLogger _logger;

        public ReservationCoordinator(
            IIdempotencyService idempotency,
            IOutboxService outbox,
            IRedisLockService lockService,
            IReservationRepository reservationRepository,
            IUnitOfWork uow,
            IAppLogger logger) // ✅ FIX
        {
            _idempotency = idempotency;
            _outbox = outbox;
            _lock = lockService;
            _reservationRepository = reservationRepository;
            _uow = uow;
            _logger = logger; // ✅ FIX
        }

        public async Task HandleAsync(CreateReservationDto dto)
        {
            if (dto.CheckOut <= dto.CheckIn)
                throw new Exception("INVALID_DATE_RANGE");

            if (string.IsNullOrWhiteSpace(dto.IdempotencyKey))
                throw new Exception("IDEMPOTENCY_KEY_REQUIRED");

            var lockKey = $"lock:room:{dto.RoomId}";
            var (acquired, token) = await _lock.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(20));

            if (!acquired)
                throw new Exception("ROOM_LOCKED");

            _logger.Info("Before UOW");

            try
            {
                var requestHash = RequestHashHelper.Compute(new
                {
                    dto.RoomId,
                    dto.UserId,
                    dto.CheckIn,
                    dto.CheckOut
                });

                var existing = await _idempotency.GetAsync(dto.IdempotencyKey);

                if (existing != null)
                {
                    if (existing.RequestHash != requestHash)
                        throw new Exception("IDEMPOTENCY_KEY_CONFLICT");

                    if (existing.Status == "PENDING")
                        throw new Exception("IN_PROGRESS");

                    if (existing.Status == "COMPLETED")
                        throw new Exception("ALREADY_COMPLETED");
                }

                await _uow.ExecuteAsync(async () =>
                {
                    _logger.Info("Inside UOW");

                    var hasConflict = await _reservationRepository.HasConflictAsync(
                        dto.RoomId,
                        dto.CheckIn,
                        dto.CheckOut
                    );

                    if (hasConflict)
                        throw new Exception("ROOM_CONFLICT");

                    await _idempotency.CreateAsync(new Idempotency
                    {
                        Key = dto.IdempotencyKey,
                        Status = "PENDING",
                        RequestHash = requestHash,
                        Response = "",
                        CreatedAt = DateTime.UtcNow
                    });

                    await _outbox.AddAsync(new OutboxMessage
                    {
                        Type = "ReservationCreated",
                        Payload = JsonSerializer.Serialize(dto),
                        CreatedAt = DateTime.UtcNow,
                        Processed = false,
                        Dead = false,
                        RetryCount = 0
                    });

                    _logger.Info($"Outbox created for RoomId: {dto.RoomId}");
                });

                _logger.Info("After UOW");

                await _idempotency.UpdateStatusAsync(
                    dto.IdempotencyKey,
                    "COMPLETED",
                    JsonSerializer.Serialize(new
                    {
                        message = "Reservation created successfully",
                        roomId = dto.RoomId,
                        userId = dto.UserId,
                        checkIn = dto.CheckIn,
                        checkOut = dto.CheckOut
                    })
                );
            }
            finally
            {
                await _lock.ReleaseLockAsync(lockKey, token);
            }
        }
    }
}