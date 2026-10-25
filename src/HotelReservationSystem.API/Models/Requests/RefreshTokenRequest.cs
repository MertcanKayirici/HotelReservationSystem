using System.ComponentModel.DataAnnotations;

namespace HotelReservationSystem.API.Models.Requests;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}