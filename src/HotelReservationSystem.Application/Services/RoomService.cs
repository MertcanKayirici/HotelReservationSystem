using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;

namespace HotelReservationSystem.Application.Services;

public class RoomService : IRoomService
{
    private readonly List<RoomDto> _rooms =
    [
        new RoomDto { Id = 1, Name = "Standart Oda", Price = 1200, Capacity = 2, IsActive = true },
        new RoomDto { Id = 2, Name = "Deluxe Oda", Price = 1800, Capacity = 3, IsActive = true },
        new RoomDto { Id = 3, Name = "Aile Odası", Price = 2500, Capacity = 4, IsActive = true }
    ];

    public Task<List<RoomDto>> GetAllAsync()
    {
        return Task.FromResult(_rooms);
    }

    public Task<RoomDto?> GetByIdAsync(int id)
    {
        var room = _rooms.FirstOrDefault(x => x.Id == id);
        return Task.FromResult(room);
    }
}