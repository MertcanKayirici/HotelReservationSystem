
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Application.Enums;

namespace HotelReservationSystem.Application.Interfaces.Repositories
{
    public interface IRoomRepository
    {
        Task<List<Room>> GetAllAsync();

        Task<Room?> GetByIdAsync(int id);

        Task AddAsync(Room room);

        Task<RoomDeletionResult> DeleteAsync(
    int id,
    CancellationToken cancellationToken = default);

        Task SaveChangesAsync();

        Task<Room?> SetActiveWithLockAsync(
            int id,
            bool isActive,
            CancellationToken cancellationToken = default);
    }
}
