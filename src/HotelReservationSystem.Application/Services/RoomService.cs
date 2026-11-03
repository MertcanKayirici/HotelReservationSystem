
using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Application.Enums;

namespace HotelReservationSystem.Application.Services
{
    public class RoomService : IRoomService
    {
        private readonly IRoomRepository _roomRepository;

        public RoomService(IRoomRepository roomRepository)
        {
            _roomRepository = roomRepository;
        }

        public async Task<List<RoomDto>> GetAllAsync()
        {
            var rooms = await _roomRepository.GetAllAsync();

            return rooms.Select(MapToDto).ToList();
        }

        public async Task<RoomDto?> GetByIdAsync(int id)
        {
            var room = await _roomRepository.GetByIdAsync(id);

            return room is null ? null : MapToDto(room);
        }

        public async Task<RoomDto> CreateAsync(CreateRoomDto dto)
        {
            var room = new Room
            {
                Name = dto.Name.Trim(),
                PricePerNight = dto.PricePerNight,
                Capacity = dto.Capacity,
                IsAvailable = dto.IsAvailable,
                IsActive = true
            };

            await _roomRepository.AddAsync(room);
            await _roomRepository.SaveChangesAsync();

            return MapToDto(room);
        }

        public async Task<RoomDto?> UpdateAsync(int id, UpdateRoomDto dto)
        {
            var room = await _roomRepository.GetByIdAsync(id);

            if (room is null)
            {
                return null;
            }

            room.Name = dto.Name.Trim();
            room.PricePerNight = dto.PricePerNight;
            room.Capacity = dto.Capacity;
            room.IsAvailable = dto.IsAvailable;

            await _roomRepository.SaveChangesAsync();

            return MapToDto(room);
        }

        public async Task<RoomDto?> ActivateAsync(int id)
        {
            var room = await _roomRepository.SetActiveWithLockAsync(
                id,
                true);

            return room is null ? null : MapToDto(room);
        }

        public async Task<RoomDto?> DeactivateAsync(int id)
        {
            var room = await _roomRepository.SetActiveWithLockAsync(
                id,
                false);

            return room is null ? null : MapToDto(room);
        }

        public async Task<RoomDeletionResult> DeleteAsync(int id)
        {
            return await _roomRepository.DeleteAsync(id);
        }

        private static RoomDto MapToDto(Room room)
        {
            return new RoomDto
            {
                Id = room.Id,
                Name = room.Name,
                PricePerNight = room.PricePerNight,
                Capacity = room.Capacity,
                IsAvailable = room.IsAvailable,
                IsActive = room.IsActive
            };
        }
    }
}
