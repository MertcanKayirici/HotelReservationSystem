using System.ComponentModel.DataAnnotations;

namespace HotelReservationSystem.Application.DTOs
{
    public class UpdateRoomDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal PricePerNight { get; set; }

        [Range(1, 20)]
        public int Capacity { get; set; }

        public bool IsAvailable { get; set; }
    }
}
