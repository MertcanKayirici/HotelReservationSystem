using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Infrastructure.Data;

namespace HotelReservationSystem.Infrastructure.UnitOfWork
{
    /// <summary>
    /// Implements Unit of Work pattern to ensure transactional consistency
    /// across multiple database operations.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        /// <summary>
        /// Database context used for transaction management and persistence.
        /// </summary>
        private readonly AppDbContext _context;

        /// <summary>
        /// Initializes the UnitOfWork with required dependencies.
        /// </summary>
        /// <param name="context">Application database context.</param>
        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Executes the provided action within a database transaction.
        /// Ensures atomicity: all operations succeed or all are rolled back.
        /// </summary>
        /// <param name="action">Asynchronous operation to execute.</param>
        public async Task ExecuteAsync(Func<Task> action)
        {
            // Begin database transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Log transaction start (debug purpose)
                Console.WriteLine("UOW START");

                // Execute business logic
                await action();

                // Persist all tracked changes
                Console.WriteLine("SAVING...");
                await _context.SaveChangesAsync();

                // Commit transaction
                Console.WriteLine("COMMIT");
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                // Rollback transaction on failure
                Console.WriteLine("ROLLBACK: " + ex.Message);

                await transaction.RollbackAsync();

                // Re-throw exception to upstream handlers
                throw;
            }
        }
    }
}