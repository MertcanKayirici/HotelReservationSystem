using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using System.Text.Json;

namespace HotelReservationSystem.Application.Services
{
    /// <summary>
    /// Implements core reservation use cases exposed to the API layer.
    /// Delegates orchestration to coordinator and manages idempotency-related behaviors.
    /// </summary>
    public class ReservationService : IReservationService
    {
        /// <summary>
        /// Coordinator responsible for handling the full reservation workflow.
        /// </summary>
        private readonly IReservationCoordinator _coordinator;

        /// <summary>
        /// Service for accessing idempotency records.
        /// </summary>
        private readonly IIdempotencyService _idempotency;

        /// <summary>
        /// Initializes the service with required dependencies.
        /// </summary>
        public ReservationService(
            IReservationCoordinator coordinator,
            IIdempotencyService idempotency)
        {
            _coordinator = coordinator;
            _idempotency = idempotency;
        }

        /// <summary>
        /// Custom exception used to indicate that a request has already been completed.
        /// Contains the cached response for immediate return.
        /// </summary>
        public class AlreadyCompletedException : Exception
        {
            /// <summary>
            /// Cached response payload from previous successful execution.
            /// </summary>
            public string CachedResponse { get; }

            public AlreadyCompletedException(string response)
            {
                CachedResponse = response;
            }
        }

        public class ReservationFailedException : Exception
        {
            public string CachedResponse { get; }

            public ReservationFailedException(string response)
                : base("RESERVATION_FAILED")
            {
                CachedResponse = response;
            }
        }

        /// <summary>
        /// Initiates reservation creation process.
        /// Handles idempotency scenario where the request has already been completed.
        /// </summary>
        /// <param name="dto">Reservation request data.</param>

        public async Task CreateReservationAsync(CreateReservationDto dto)
        {
            try
            {
                await _coordinator.HandleAsync(dto);
            }
            catch (Exception ex) when (
                ex.Message == "ALREADY_COMPLETED" ||
                ex.Message == "RESERVATION_FAILED")
            {
                var record = await _idempotency.GetAsync(dto.IdempotencyKey);

                if (record == null)
                    throw;

                if (ex.Message == "ALREADY_COMPLETED")
                {
                    throw new AlreadyCompletedException(
                        record.Response ?? string.Empty);
                }

                throw new ReservationFailedException(
                    record.Response ?? string.Empty);
            }
        }

        /// <summary>
        /// Retrieves the current status of a reservation request.
        /// </summary>
        /// <param name="requestId">Idempotency key used to track the request.</param>
        /// <returns>Status and optional response payload.</returns>
        public async Task<ReservationStatusResult> GetStatusAsync(string requestId)
        {
            // Fetch idempotency record
            var record = await _idempotency.GetAsync(requestId);

            // Throw if no record exists for given request
            if (record == null)
                throw new Exception("NOT_FOUND");

            // Map domain record to DTO response
            return new ReservationStatusResult
            {
                Status = record.Status,
                Response = record.Response
            };
        }
    }
}