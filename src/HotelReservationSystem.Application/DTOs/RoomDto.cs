namespace HotelReservationSystem.Application.DTOs
{
    public class RoomDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal PricePerNight { get; set; }

        public int Capacity { get; set; }

        public bool IsAvailable { get; set; }
    }
}
