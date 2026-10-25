using HotelReservationSystem.Application.DTOs;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Interfaces
{
    /// <summary>
    /// Defines core reservation-related business operations exposed to the API layer.
    /// Acts as an entry point for handling reservation use cases.
    /// </summary>
    public interface IReservationService
    {
        /// <summary>
        /// Retrieves the current status of a reservation request by its identifier.
        /// </summary>
        /// <param name="requestId">Unique request identifier (idempotency key).</param>
        /// <returns>Reservation status result containing state and optional response data.</returns>
        Task<ReservationStatusResult> GetStatusAsync(string requestId, int userId);

        /// <summary>
        /// Initiates the reservation creation process.
        /// Typically triggers asynchronous processing via coordinator and background workflows.
        /// </summary>
        /// <param name="dto">Reservation creation request data.</param>
        Task CreateReservationAsync(CreateReservationDto dto);
    }
}