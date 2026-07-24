using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Inventory.Domain;

namespace Inventory.Infrastructure;

/// <summary>
/// Persistence mapping for reservations and their lines.
/// </summary>
public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Reservation> entity)
    {
        // Table
        entity.ToTable("reservations");

        // Primary Key
        entity.HasKey(x => x.Id);

        // Properties
        entity.Ignore(x => x.DomainEvents);
        entity.Property(x => x.Version).IsConcurrencyToken();
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);

        // Relationships
        entity.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.ReservationId);
        entity.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        entity.HasIndex(x => x.OrderId).IsUnique();
    }
}
