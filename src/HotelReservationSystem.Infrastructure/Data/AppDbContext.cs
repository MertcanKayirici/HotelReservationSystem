
using Microsoft.EntityFrameworkCore;
using HotelReservationSystem.Domain.Entities;

namespace HotelReservationSystem.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<OutboxMessage> OutboxMessages { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Idempotency> Idempotencies { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
