namespace HotelReservationSystem.Application.DTOs
{
    /// <summary>
    /// Represents the result returned from the application layer when querying
    /// the status of a reservation request.
    /// </summary>
    public class ReservationStatusResult
    {
        /// <summary>
        /// Current state of the reservation process (e.g., IN_PROGRESS, COMPLETED, FAILED).
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Optional serialized response payload associated with the reservation.
        /// Typically populated when the process is completed.
        /// </summary>
        public string? Response { get; set; }
    }
}