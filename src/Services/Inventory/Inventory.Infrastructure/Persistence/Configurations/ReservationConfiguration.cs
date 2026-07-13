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
        entity.ToTable("reservations").HasKey(x => x.Id);
        entity.Ignore(x => x.DomainEvents);
        entity.Ignore(x => x.UpdatedAt);
        entity.Ignore(x => x.Version);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        entity.HasIndex(x => x.OrderId).IsUnique();
        entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.ReservationId);
        entity.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
