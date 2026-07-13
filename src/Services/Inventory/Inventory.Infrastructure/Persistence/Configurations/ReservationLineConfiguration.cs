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
        entity.ToTable("reservation_lines").HasKey(x => x.Id);
    }
}
