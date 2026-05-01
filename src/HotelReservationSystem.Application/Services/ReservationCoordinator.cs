using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Application.Common;
using HotelReservationSystem.Domain.Entities;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Services
{
    public class ReservationCoordinator : IReservationCoordinator
    {
        private readonly IIdempotencyService _idempotency;
        private readonly IOutboxService _outbox;
        private readonly IRedisLockService _lock;

        public ReservationCoordinator(
            IIdempotencyService idempotency,
            IOutboxService outbox,
            IRedisLockService lockService)
        {
            _idempotency = idempotency;
            _outbox = outbox;
            _lock = lockService;
        }

        public async Task HandleAsync(CreateReservationDto dto)
        {
            // 🔹 VALIDATION
            if (dto.CheckOut <= dto.CheckIn)
                throw new Exception("INVALID_DATE_RANGE");

            if (string.IsNullOrWhiteSpace(dto.IdempotencyKey))
                throw new Exception("IDEMPOTENCY_KEY_REQUIRED");

            var lockKey = $"lock:room:{dto.RoomId}";

            // 🔥 TOKEN'LI LOCK
            var (acquired, token) = await _lock.AcquireLockAsync(
                lockKey,
                TimeSpan.FromSeconds(20)
            );

            if (!acquired)
                throw new Exception("ROOM_LOCKED");

            try
            {
                // 🔹 REQUEST HASH (SHA256)
                var requestHash = RequestHashHelper.Compute(new
                {
                    dto.RoomId,
                    dto.UserId,
                    dto.CheckIn,
                    dto.CheckOut
                });

                // 🔍 IDEMPOTENCY CHECK
                var existing = await _idempotency.GetAsync(dto.IdempotencyKey);

                if (existing != null)
                {
                    // 🔥 EN KRİTİK: HASH CHECK
                    if (existing.RequestHash != requestHash)
                        throw new Exception("IDEMPOTENCY_KEY_CONFLICT");

                    if (existing.Status == "PENDING")
                        throw new Exception("IN_PROGRESS");

                    if (existing.Status == "COMPLETED")
                        return;
                }

                // 🔥 CREATE IDEMPOTENCY
                try
                {
                    await _idempotency.CreateAsync(new Idempotency
                    {
                        Key = dto.IdempotencyKey,
                        Status = "PENDING",
                        RequestHash = requestHash,
                        Response = "",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                catch
                {
                    throw new Exception("IN_PROGRESS");
                }

                // 🔥 OUTBOX EVENT
                await _outbox.AddAsync(new OutboxMessage
                {
                    Type = "ReservationCreated",
                    Payload = JsonSerializer.Serialize(dto),
                    CreatedAt = DateTime.UtcNow,
                    Processed = false
                });

                await Task.Delay(300);

                // 🔥 FINAL STATE
                await _idempotency.UpdateStatusAsync(
                    dto.IdempotencyKey,
                    "COMPLETED",
                    "Reservation created successfully"
                );
            }
            finally
            {
                try
                {
                    await _lock.ReleaseLockAsync(lockKey, token);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }
}