namespace HotelReservationSystem.Domain.Entities
{
    public class OutboxMessage
    {
        public int Id { get; set; }

        public string Type { get; set; }
        public string Payload { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool Processed { get; set; }
    }
}