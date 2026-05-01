using HotelReservationSystem.Application.Common;
using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Domain.Entities;
using System;
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

        public ReservationCoordinator(
            IIdempotencyService idempotency,
            IOutboxService outbox,
            IRedisLockService lockService,
            IReservationRepository reservationRepository,
            IUnitOfWork uow)
        {
            _idempotency = idempotency;
            _outbox = outbox;
            _lock = lockService;
            _reservationRepository = reservationRepository;
            _uow = uow;
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
                        return;
                }

                // 🔥 TRANSACTION BLOCK
                await _uow.ExecuteAsync(async () =>
                {
                    await _idempotency.CreateAsync(new Idempotency
                    {
                        Key = dto.IdempotencyKey,
                        Status = "PENDING",
                        RequestHash = requestHash,
                        Response = "", // 🔥 KRİTİK FIX
                        CreatedAt = DateTime.UtcNow
                    });

                    // (normalde burası çalışmayacak)
                    await _reservationRepository.AddAsync(new Reservation
                    {
                        RoomId = dto.RoomId,
                        UserId = dto.UserId,
                        CheckInDate = dto.CheckIn,
                        CheckOutDate = dto.CheckOut
                    });
                });

                // 🔥 BURASI TESTTE ÇALIŞMAZ (doğru)
                await _idempotency.UpdateStatusAsync(
                    dto.IdempotencyKey,
                    "COMPLETED",
                    "Reservation created successfully"
                );
            }
            finally
            {
                await _lock.ReleaseLockAsync(lockKey, token);
            }
        }
    }
}