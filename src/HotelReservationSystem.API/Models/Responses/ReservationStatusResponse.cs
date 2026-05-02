namespace HotelReservationSystem.API.Models.Responses
{
    /// <summary>
    /// Represents the response payload for querying reservation status.
    /// Encapsulates both the current state and any associated result data.
    /// </summary>
    public class ReservationStatusResponse
    {
        /// <summary>
        /// Current status of the reservation request (e.g., IN_PROGRESS, COMPLETED, FAILED).
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Optional payload containing additional data related to the reservation.
        /// This may vary depending on the status (e.g., completed reservation details).
        /// </summary>
        public object? Data { get; set; }
    }
}