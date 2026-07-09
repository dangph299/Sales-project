using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Inventory.Domain;

namespace Inventory.Infrastructure;

/// <summary>
/// EF Core mapping for <see cref="ReservationLine"/>: table configuration.
/// </summary>
public sealed class ReservationLineConfiguration : IEntityTypeConfiguration<ReservationLine>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ReservationLine> entity)
    {
        entity.ToTable("reservation_lines").HasKey(x => x.Id);
    }
}
