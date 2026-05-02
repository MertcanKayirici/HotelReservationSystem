using HotelReservationSystem.Application.DTOs;

namespace HotelReservationSystem.Application.Interfaces;

public interface IRoomService
{
    Task<List<RoomDto>> GetAllAsync();
    Task<RoomDto?> GetByIdAsync(int id);
}