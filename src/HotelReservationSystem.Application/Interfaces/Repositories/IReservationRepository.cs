using HotelReservationSystem.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Interfaces.Repositories
{
    public interface IReservationRepository
    {
        Task<bool> ExistsConflictAsync(int roomId, DateTime checkIn, DateTime checkOut);
        Task AddAsync(Reservation reservation);
        Task<bool> HasConflictAsync(int roomId, DateTime checkIn, DateTime checkOut);
    }
}