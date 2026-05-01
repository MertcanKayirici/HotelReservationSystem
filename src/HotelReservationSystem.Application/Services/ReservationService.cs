using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using System.Text.Json;

namespace HotelReservationSystem.Application.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IReservationCoordinator _coordinator;
        private readonly IIdempotencyService _idempotency;

        public ReservationService(
            IReservationCoordinator coordinator,
            IIdempotencyService idempotency)
        {
            _coordinator = coordinator;
            _idempotency = idempotency;
        }

        public class AlreadyCompletedException : Exception
        {
            public string CachedResponse { get; }

            public AlreadyCompletedException(string response)
            {
                CachedResponse = response;
            }
        }

        public async Task CreateReservationAsync(CreateReservationDto dto)
        {
            try
            {
                await _coordinator.HandleAsync(dto);
            }
            catch (Exception ex) when (ex.Message == "ALREADY_COMPLETED")
            {
                var record = await _idempotency.GetAsync(dto.IdempotencyKey);

                if (record != null)
                {
                    throw new AlreadyCompletedException(record.Response);
                }

                throw;
            }
        }


        public async Task<ReservationStatusResult> GetStatusAsync(string requestId)
        {
            var record = await _idempotency.GetAsync(requestId);

            if (record == null)
                throw new Exception("NOT_FOUND");

            return new ReservationStatusResult
            {
                Status = record.Status,
                Response = record.Response
            };
        }
    }
}