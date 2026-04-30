using HotelReservationSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelReservationSystem.Infrastructure.Persistence.Configurations
{
    public class IdempotencyConfiguration : IEntityTypeConfiguration<Idempotency>
    {
        public void Configure(EntityTypeBuilder<Idempotency> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Key)
                   .IsRequired();

            builder.HasIndex(x => x.Key)
                   .IsUnique();

            builder.Property(x => x.Status)
                   .IsRequired();
        }
    }
}
