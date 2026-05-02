using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;

namespace HotelReservationSystem.Application.Services;

public class RoomService : IRoomService
{
    private static readonly List<RoomDto> _rooms =
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

    public Task<RoomDto> CreateAsync(CreateRoomDto dto)
    {
        var newId = _rooms.Count == 0 ? 1 : _rooms.Max(x => x.Id) + 1;

        var room = new RoomDto
        {
            Id = newId,
            Name = dto.Name,
            Price = dto.Price,
            Capacity = dto.Capacity,
            IsActive = dto.IsActive
        };

        _rooms.Add(room);

        return Task.FromResult(room);
    }

    public Task<RoomDto?> UpdateAsync(int id, UpdateRoomDto dto)
    {
        var room = _rooms.FirstOrDefault(x => x.Id == id);

        if (room is null)
        {
            return Task.FromResult<RoomDto?>(null);
        }

        room.Name = dto.Name;
        room.Price = dto.Price;
        room.Capacity = dto.Capacity;
        room.IsActive = dto.IsActive;

        return Task.FromResult<RoomDto?>(room);
    }

    public Task<bool> DeleteAsync(int id)
    {
        var room = _rooms.FirstOrDefault(x => x.Id == id);

        if (room is null)
        {
            return Task.FromResult(false);
        }

        _rooms.Remove(room);

        return Task.FromResult(true);
    }
}