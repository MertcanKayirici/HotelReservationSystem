using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository implementation for managing reservation data persistence.
/// Handles database operations related to reservations.
/// </summary>
public class ReservationRepository : IReservationRepository
{
    /// <summary>
    /// Database context for accessing reservation data.
    /// </summary>
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes the repository with required dependencies.
    /// </summary>
    /// <param name="context">Application database context.</param>
    public ReservationRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Adds a new reservation to the database and persists changes immediately.
    /// </summary>
    /// <param name="reservation">Reservation entity to be added.</param>
    public async Task AddAsync(Reservation reservation)
    {
        // Add reservation entity to DbContext
        await _context.Reservations.AddAsync(reservation);

        // Persist changes immediately
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Checks if there is any conflicting reservation for the given room and date range.
    /// Uses overlap detection logic.
    /// </summary>
    /// <param name="roomId">Target room identifier.</param>
    /// <param name="checkIn">Requested check-in date.</param>
    /// <param name="checkOut">Requested check-out date.</param>
    /// <returns>True if a conflict exists; otherwise false.</returns>
    public async Task<bool> HasConflictAsync(int roomId, DateTime checkIn, DateTime checkOut)
    {
        // Detect overlapping reservations for the same room
        return await _context.Reservations.AnyAsync(x =>
            x.RoomId == roomId &&
            !(checkOut <= x.CheckInDate || checkIn >= x.CheckOutDate)
        );
    }

    /// <summary>
    /// Checks for existing reservation conflicts using similar overlap logic.
    /// May serve different use-case semantics depending on context.
    /// </summary>
    /// <param name="roomId">Target room identifier.</param>
    /// <param name="checkIn">Requested check-in date.</param>
    /// <param name="checkOut">Requested check-out date.</param>
    /// <returns>True if a conflict exists; otherwise false.</returns>
    public async Task<bool> ExistsConflictAsync(int roomId, DateTime checkIn, DateTime checkOut)
    {
        // Detect overlapping reservations for the same room
        return await _context.Reservations.AnyAsync(r =>
            r.RoomId == roomId &&
            !(checkOut <= r.CheckInDate || checkIn >= r.CheckOutDate)
        );
    }
}