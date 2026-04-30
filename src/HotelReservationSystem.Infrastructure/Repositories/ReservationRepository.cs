using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Data;

namespace HotelReservationSystem.Infrastructure.Repositories
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly AppDbContext _context;

        public ReservationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> TryCreateReservationAsync(Reservation reservation)
        {
            await using var transaction = await _context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);

            var hasConflict = await _context.Reservations.AnyAsync(r =>
                r.RoomId == reservation.RoomId &&
                reservation.CheckInDate < r.CheckOutDate &&
                reservation.CheckOutDate > r.CheckInDate
            );

            if (hasConflict)
            {
                await transaction.RollbackAsync();
                return false;
            }

            await _context.Reservations.AddAsync(reservation);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return true;
        }

        public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut)
        {
            return !await _context.Reservations.AnyAsync(r =>
                r.RoomId == roomId &&
                checkIn < r.CheckOutDate &&
                checkOut > r.CheckInDate
            );
        }

        public async Task AddAsync(Reservation reservation)
        {
            await _context.Reservations.AddAsync(reservation);
            await _context.SaveChangesAsync();
        }
    }
}