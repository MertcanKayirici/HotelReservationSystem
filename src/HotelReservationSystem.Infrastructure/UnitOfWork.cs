using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Infrastructure.Data;

namespace HotelReservationSystem.Infrastructure
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
            using var transaction = await _context.Database.BeginTransactionAsync();

            await action();

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
    }
}