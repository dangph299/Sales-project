using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Inventory.Domain;

namespace Inventory.Infrastructure;

/// <summary>
/// Persistence mapping for reservation lines.
/// </summary>
public sealed class ReservationLineConfiguration : IEntityTypeConfiguration<ReservationLine>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ReservationLine> entity)
    {
        // Table
        entity.ToTable("reservation_lines");

        // Primary Key
        entity.HasKey(x => x.Id);
    }
}
