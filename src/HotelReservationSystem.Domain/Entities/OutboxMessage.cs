namespace HotelReservationSystem.Domain.Entities
{
    /// <summary>
    /// Represents an outbox message used in the Outbox Pattern
    /// to ensure reliable event publishing and eventual consistency.
    /// </summary>
    public class OutboxMessage
    {
        /// <summary>
        /// Primary key of the outbox message.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Logical type of the event (e.g., ReservationCreated).
        /// Used by consumers to determine how to process the message.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Serialized payload of the event.
        /// Typically contains the data required for downstream processing.
        /// </summary>
        public string Payload { get; set; }

        /// <summary>
        /// Timestamp when the message was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Indicates whether the message has been successfully processed and published.
        /// </summary>
        public bool Processed { get; set; }

        /// <summary>
        /// Number of retry attempts made to process this message.
        /// Used for retry and failure handling strategies.
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// Indicates whether the message is considered dead (moved to DLQ or permanently failed).
        /// </summary>
        public bool Dead { get; set; } = false;
    }
}