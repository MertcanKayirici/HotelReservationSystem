using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Domain.Entities;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Services
{
    public class ReservationCoordinator : IReservationCoordinator
    {
        private readonly IIdempotencyService _idempotency;

        public ReservationCoordinator(IIdempotencyService idempotency)
        {
            _idempotency = idempotency;
        }

        public async Task HandleAsync(CreateReservationDto dto)
        {
            // 🔹 VALIDATION
            if (dto.CheckOut <= dto.CheckIn)
                throw new Exception("INVALID_DATE");

            if (string.IsNullOrEmpty(dto.IdempotencyKey))
                throw new Exception("Idempotency key required");

            // 🔍 IDEMPOTENCY CHECK
            var existing = await _idempotency.GetAsync(dto.IdempotencyKey);

            if (existing != null)
            {
                if (existing.Status == "PENDING")
                    throw new Exception("IN_PROGRESS");

                if (existing.Status == "COMPLETED")
                    return;
            }

            // 🔹 REQUEST HASH
            var requestHash = JsonSerializer.Serialize(new
            {
                dto.RoomId,
                dto.UserId,
                dto.CheckIn,
                dto.CheckOut
            });

            // 🔥 CREATE REQUEST
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

            // 🔥 FAKE PROCESS (Kafka yerine)
            await Task.Delay(500);

            await _idempotency.UpdateStatusAsync(
                dto.IdempotencyKey,
                "COMPLETED",
                "Reservation created successfully"
            );
        }
    }
}