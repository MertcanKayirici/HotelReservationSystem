using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Enums;

namespace HotelReservationSystem.Application.Interfaces
{
    public interface IRoomService
    {
        Task<List<RoomDto>> GetAllAsync();

        Task<RoomDto?> GetByIdAsync(int id);

        Task<RoomDto> CreateAsync(CreateRoomDto dto);

        Task<RoomDto?> UpdateAsync(int id, UpdateRoomDto dto);

        Task<RoomDto?> ActivateAsync(int id);

        Task<RoomDto?> DeactivateAsync(int id);

        Task<RoomDeletionResult> DeleteAsync(int id);
    }
}