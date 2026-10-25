namespace HotelReservationSystem.Domain.Entities
{
    /// <summary>
    /// Represents an idempotency record used to ensure that duplicate requests
    /// are handled safely and consistently across the system.
    /// </summary>
    public class Idempotency
    {
        /// <summary>
        /// Primary key of the idempotency record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Unique idempotency key provided by the client.
        /// Used to identify and deduplicate requests.
        /// </summary>
        public string Key { get; set; } = default!;

        /// <summary>
        /// Current processing status of the request (e.g., PENDING, COMPLETED, FAILED).
        /// </summary>
        public string Status { get; set; } = default!;

        /// <summary>
        /// Hash of the original request payload.
        /// Used to detect conflicts when the same key is reused with different data.
        /// </summary>
        public string? RequestHash { get; set; }

        /// <summary>
        /// Serialized response payload associated with the request.
        /// Stored to return consistent results for repeated calls.
        /// </summary>
        public string? Response { get; set; }

        /// <summary>
        /// Timestamp indicating when the idempotency record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Identifier of the authenticated user who owns this request.
        /// Null is permitted for legacy records with unknown ownership.
        /// </summary>
        public int? UserId { get; set; }
    }
}