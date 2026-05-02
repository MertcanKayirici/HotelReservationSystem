using System;

namespace HotelReservationSystem.Application.DTOs
{
    /// <summary>
    /// Data Transfer Object used for creating a new reservation request.
    /// Carries all required information from the client to the application layer.
    /// </summary>
    public class CreateReservationDto
    {
        /// <summary>
        /// Identifier of the room to be reserved.
        /// </summary>
        public int RoomId { get; set; }

        /// <summary>
        /// Identifier of the user making the reservation.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Reservation start date.
        /// </summary>
        public DateTime CheckIn { get; set; }

        /// <summary>
        /// Reservation end date.
        /// Must be greater than CheckIn.
        /// </summary>
        public DateTime CheckOut { get; set; }

        /// <summary>
        /// Unique key used to guarantee idempotent request handling.
        /// Prevents duplicate reservation processing.
        /// </summary>
        public string IdempotencyKey { get; set; }

        /// <summary>
        /// Correlation identifier used for tracing requests across distributed systems.
        /// Typically injected via middleware.
        /// </summary>
        public string? CorrelationId { get; set; }
    }
}