using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class ReservationRepository : IReservationRepository
{
    private readonly AppDbContext _context;

    public ReservationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Reservation reservation)
    {
        await _context.Reservations.AddAsync(reservation);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> HasConflictAsync(int roomId, DateTime checkIn, DateTime checkOut)
    {
        return await _context.Reservations.AnyAsync(x =>
            x.RoomId == roomId &&
            !(checkOut <= x.CheckInDate || checkIn >= x.CheckOutDate)
        );
    }

    public async Task<bool> ExistsConflictAsync(int roomId, DateTime checkIn, DateTime checkOut)
    {
        return await _context.Reservations.AnyAsync(r =>
            r.RoomId == roomId &&
            !(checkOut <= r.CheckInDate || checkIn >= r.CheckOutDate)
        );
    }
}