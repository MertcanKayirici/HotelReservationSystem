namespace HotelReservationSystem.Domain.Entities
{
    public class Idempotency
    {
        public int Id { get; set; }

        public string Key { get; set; } = default!;
        public string Status { get; set; } = default!;

        public string? RequestHash { get; set; }
        public string? Response { get; set; }  // 🔥 düzeltildi

        public DateTime CreatedAt { get; set; }
    }
}