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

            try
            {
                Console.WriteLine("🟡 UOW START");

                await action();

                Console.WriteLine("💾 SAVING...");

                await _context.SaveChangesAsync();

                Console.WriteLine("✅ COMMIT");

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ ROLLBACK: " + ex.Message);

                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}