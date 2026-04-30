using System;

namespace HotelReservationSystem.Application.DTOs
{
    public class CreateReservationDto
    {
        public int RoomId { get; set; }
        public int UserId { get; set; }

        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }

        public string IdempotencyKey { get; set; }
    }
}