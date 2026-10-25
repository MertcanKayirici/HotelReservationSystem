using HotelReservationSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelReservationSystem.Infrastructure.Persistence.Configurations
{
    public class RoomConfiguration : IEntityTypeConfiguration<Room>
    {
        public void Configure(EntityTypeBuilder<Room> builder)
        {
            builder.ToTable("Rooms");

            builder.HasKey(room => room.Id);

            builder.Property(room => room.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(room => room.PricePerNight)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(room => room.Capacity)
                .IsRequired();

            builder.Property(room => room.IsAvailable)
                .IsRequired();
        }
    }
}
