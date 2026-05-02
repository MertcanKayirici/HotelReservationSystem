namespace HotelReservationSystem.Domain.Entities
{
    /// <summary>
    /// Represents a reservation entity stored in the system.
    /// Contains booking details for a specific room and user.
    /// </summary>
    public class Reservation
    {
        /// <summary>
        /// Primary key of the reservation.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identifier of the reserved room.
        /// </summary>
        public int RoomId { get; set; }

        /// <summary>
        /// Identifier of the user who made the reservation.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Reservation start date.
        /// </summary>
        public DateTime CheckInDate { get; set; }

        /// <summary>
        /// Reservation end date.
        /// </summary>
        public DateTime CheckOutDate { get; set; }

        /// <summary>
        /// Timestamp indicating when the reservation was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}