namespace HotelReservationSystem.API.Models.Responses
{
    /// <summary>
    /// Represents the response returned after initiating a reservation request.
    /// Provides tracking information for asynchronous processing.
    /// </summary>
    public class CreateReservationResponse
    {
        /// <summary>
        /// Initial status of the reservation request.
        /// Defaults to PENDING as the process is handled asynchronously.
        /// </summary>
        public string Status { get; set; } = "PENDING";

        /// <summary>
        /// Unique identifier used to track the reservation request status.
        /// Typically corresponds to an idempotency key or request correlation value.
        /// </summary>
        public string RequestId { get; set; } = string.Empty;
    }
}