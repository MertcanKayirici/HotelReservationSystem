namespace HotelReservationSystem.Application.DTOs;

public class RoomDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Capacity { get; set; }

    public bool IsActive { get; set; }
}