using HotelReservationSystem.Application.DTOs;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Interfaces
{
    /// <summary>
    /// Coordinates the end-to-end reservation workflow.
    /// Acts as an orchestration layer between services, ensuring correct execution order
    /// and consistency across operations (e.g., locking, validation, persistence, messaging).
    /// </summary>
    public interface IReservationCoordinator
    {
        /// <summary>
        /// Handles the reservation process for a given request.
        /// Encapsulates all required steps such as validation, idempotency checks,
        /// locking, and event publishing.
        /// </summary>
        /// <param name="dto">Reservation creation request data.</param>
        Task HandleAsync(CreateReservationDto dto);
    }
}