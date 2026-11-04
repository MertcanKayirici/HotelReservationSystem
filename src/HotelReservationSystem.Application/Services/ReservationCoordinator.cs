
using System.Text.Json;
using HotelReservationSystem.Application.Common;
using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Exceptions;
using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Domain.Entities;

namespace HotelReservationSystem.Application.Services
{
    /// <summary>
    /// Orchestrates the full reservation workflow.
    /// </summary>
    public class ReservationCoordinator : IReservationCoordinator
    {
        private readonly IIdempotencyService _idempotency;
        private readonly IOutboxService _outbox;
        private readonly IRedisLockService _lock;
        private readonly IReservationRepository _reservationRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly IUnitOfWork _uow;
        private readonly IAppLogger _logger;

        public ReservationCoordinator(
            IIdempotencyService idempotency,
            IOutboxService outbox,
            IRedisLockService lockService,
            IReservationRepository reservationRepository,
            IRoomRepository roomRepository,
            IUnitOfWork uow,
            IAppLogger logger)
        {
            _idempotency = idempotency;
            _outbox = outbox;
            _lock = lockService;
            _reservationRepository = reservationRepository;
            _roomRepository = roomRepository;
            _uow = uow;
            _logger = logger;
        }

        public async Task HandleAsync(CreateReservationDto dto)
        {
            if (dto.CheckOut <= dto.CheckIn)
                throw new Exception("INVALID_DATE_RANGE");

            if (string.IsNullOrWhiteSpace(dto.IdempotencyKey))
                throw new Exception("IDEMPOTENCY_KEY_REQUIRED");

            var lockKey = $"lock:room:{dto.RoomId}";

            var (acquired, token) = await _lock.AcquireLockAsync(
                lockKey,
                TimeSpan.FromSeconds(20));

            if (!acquired)
                throw new Exception("ROOM_LOCKED");

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

                    if (existing.Status == "FAILED")
                        throw new Exception("RESERVATION_FAILED");

                    throw new Exception("IDEMPOTENCY_STATUS_INVALID");
                }

                var room = await _roomRepository.GetByIdAsync(dto.RoomId);

                if (room == null)
                    throw new Exception("ROOM_NOT_FOUND");

                if (!room.IsActive)
                    throw new Exception("ROOM_INACTIVE");

                _logger.Info("Inside UOW for {RoomId}", dto.RoomId);

                try
                {
                    await _uow.ExecuteAsync(async () =>
                    {
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

                        _logger.Info(
                            $"Outbox created for RoomId: {dto.RoomId}");
                    });
                }
                catch (IdempotencyKeyAlreadyExistsException)
                {
                    var conflictingRecord =
                        await _idempotency.GetAsync(dto.IdempotencyKey);

                    if (conflictingRecord == null)
                        throw;

                    if (conflictingRecord.RequestHash != requestHash)
                        throw new Exception("IDEMPOTENCY_KEY_CONFLICT");

                    if (conflictingRecord.Status == "PENDING")
                        throw new Exception("IN_PROGRESS");

                    if (conflictingRecord.Status == "COMPLETED")
                        throw new Exception("ALREADY_COMPLETED");

                    if (conflictingRecord.Status == "FAILED")
                        throw new Exception("RESERVATION_FAILED");

                    throw new Exception("IDEMPOTENCY_STATUS_INVALID");
                }

                _logger.Info(
                    "After UOW for {RoomId}",
                    dto.RoomId);
            }
            finally
            {
                await _lock.ReleaseLockAsync(lockKey, token);
            }
        }
    }
}
