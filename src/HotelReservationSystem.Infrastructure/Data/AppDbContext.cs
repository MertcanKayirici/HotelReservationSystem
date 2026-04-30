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

        public DbSet<Reservation> Reservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Reservation>()
                .HasIndex(r => new { r.RoomId, r.CheckInDate, r.CheckOutDate });

            base.OnModelCreating(modelBuilder);
        }
    }
}