using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Inventory.Domain;

namespace Inventory.Infrastructure;

/// <summary>
/// EF Core mapping for <see cref="Reservation"/>: table/index/column configuration, including the
/// unique constraint on <c>OrderId</c> (one reservation per order) and the owned
/// <see cref="ReservationLine"/> collection.
/// </summary>
public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Reservation> entity)
    {
        entity.ToTable("reservations").HasKey(x => x.Id);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        entity.HasIndex(x => x.OrderId).IsUnique();
        entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.ReservationId);
        entity.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
