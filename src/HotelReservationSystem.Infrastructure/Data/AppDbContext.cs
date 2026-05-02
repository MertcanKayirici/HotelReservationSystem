using Microsoft.EntityFrameworkCore;
using HotelReservationSystem.Domain.Entities;

namespace HotelReservationSystem.Infrastructure.Data
{
    /// <summary>
    /// Entity Framework Core database context representing the application's data store.
    /// Manages entity sets and database configurations.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Initializes the database context with configured options.
        /// </summary>
        /// <param name="options">Database context configuration options.</param>
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Represents the OutboxMessages table used for event publishing.
        /// </summary>
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        /// <summary>
        /// Represents the Reservations table storing booking data.
        /// </summary>
        public DbSet<Reservation> Reservations { get; set; }

        /// <summary>
        /// Represents the Idempotencies table used for request deduplication.
        /// </summary>
        public DbSet<Idempotency> Idempotencies { get; set; }

        public DbSet<User> Users { get; set; }

        /// <summary>
        /// Configures entity mappings and applies all configurations from the assembly.
        /// </summary>
        /// <param name="modelBuilder">Model builder instance.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Automatically apply all IEntityTypeConfiguration implementations in the assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}