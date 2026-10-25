
using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Infrastructure.Data;
using HotelReservationSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using HotelReservationSystem.Application.Exceptions;

namespace HotelReservationSystem.Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public async Task ExecuteAsync(Func<Task> action)
        {
            var previouslyAdded = _context.ChangeTracker
                .Entries()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToHashSet();

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                await action();
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                if (ex is DbUpdateException dbEx &&
                    SqlServerIdempotencyConflictDetector
                        .IsIdempotencyKeyConflict(dbEx))
                {
                    foreach (var entry in _context.ChangeTracker.Entries()
                                 .Where(e =>
                                     e.State == EntityState.Added &&
                                     !previouslyAdded.Contains(e.Entity))
                                 .ToList())
                    {
                        entry.State = EntityState.Detached;
                    }

                    throw new IdempotencyKeyAlreadyExistsException(dbEx);
                }

                throw;
            }
        }
    }
}
