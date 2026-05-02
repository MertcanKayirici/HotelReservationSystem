using HotelReservationSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelReservationSystem.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Entity Framework configuration for the Idempotency entity.
    /// Defines schema rules, constraints, and indexing strategy.
    /// </summary>
    public class IdempotencyConfiguration : IEntityTypeConfiguration<Idempotency>
    {
        /// <summary>
        /// Configures the Idempotency entity mapping.
        /// </summary>
        /// <param name="builder">Entity type builder.</param>
        public void Configure(EntityTypeBuilder<Idempotency> builder)
        {
            // Define primary key
            builder.HasKey(x => x.Id);

            // Configure Key property as required
            builder.Property(x => x.Key)
                   .IsRequired();

            // Enforce uniqueness on idempotency key to prevent duplicates
            builder.HasIndex(x => x.Key)
                   .IsUnique();

            // Configure Status property as required
            builder.Property(x => x.Status)
                   .IsRequired();
        }
    }
}