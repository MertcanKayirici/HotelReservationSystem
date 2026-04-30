using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Domain.Entities;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IIdempotencyService _idempotency;

        public ReservationService(IIdempotencyService idempotency)
        {
            _idempotency = idempotency;
        }

        public async Task CreateReservationAsync(CreateReservationDto dto)
        {
            // 🔹 VALIDATION
            if (dto.CheckOut <= dto.CheckIn)
                throw new Exception("INVALID_DATE");

            if (string.IsNullOrEmpty(dto.IdempotencyKey))
                throw new Exception("Idempotency key required");

            // 🔍 EXISTING CHECK
            var existing = await _idempotency.GetAsync(dto.IdempotencyKey);

            if (existing != null)
            {
                if (existing.Status == "PENDING")
                    throw new Exception("IN_PROGRESS");

                if (existing.Status == "COMPLETED")
                    return;
            }

            // 🔥 REQUEST HASH (EN KRİTİK FIX)
            var requestHash = JsonSerializer.Serialize(new
            {
                dto.RoomId,
                dto.UserId,
                dto.CheckIn,
                dto.CheckOut
            });

            // 🔥 INSERT
            try
            {
                await _idempotency.CreateAsync(new Idempotency
                {
                    Key = dto.IdempotencyKey,
                    Status = "PENDING",
                    RequestHash = requestHash,   // ✅ artık NULL değil
                    Response = "",               // ✅ DB boş kabul etmiyorsa
                    CreatedAt = DateTime.UtcNow
                });

                // 🔥 FAKE PROCESSING (Kafka yerine)
                await Task.Delay(500);

                await _idempotency.UpdateStatusAsync(
                    dto.IdempotencyKey,
                    "COMPLETED",
                    "Reservation created successfully"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine("REAL ERROR: " + ex.ToString());

                // 🔥 duplicate / race condition fallback
                throw new Exception("IN_PROGRESS");
            }
        }
    }
}