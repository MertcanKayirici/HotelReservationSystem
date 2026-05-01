namespace HotelReservationSystem.Domain.Entities
{
    public class Idempotency
    {
        public int Id { get; set; }

        public string Key { get; set; }
        public string Status { get; set; }

        public string? RequestHash { get; set; }
        public string Response { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
