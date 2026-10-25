using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Domain.Entities;

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
                IsAvailable = dto.IsAvailable
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

        public async Task<bool> DeleteAsync(int id)
        {
            var deleted = await _roomRepository.DeleteAsync(id);

            if (!deleted)
            {
                return false;
            }

            await _roomRepository.SaveChangesAsync();
            return true;
        }

        private static RoomDto MapToDto(Room room)
        {
            return new RoomDto
            {
                Id = room.Id,
                Name = room.Name,
                PricePerNight = room.PricePerNight,
                Capacity = room.Capacity,
                IsAvailable = room.IsAvailable
            };
        }
    }
}
