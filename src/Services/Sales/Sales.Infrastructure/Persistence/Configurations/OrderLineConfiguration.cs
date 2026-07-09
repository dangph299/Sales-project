using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// EF Core mapping for <see cref="OrderLine"/>: table/index/column configuration, including the
/// <see cref="Money"/> value converter and the unique (OrderId, ProductId) constraint.
/// </summary>
public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<OrderLine> entity)
    {
        var money = new ValueConverter<Money, decimal>(x => x.Amount, x => Money.Vnd(x));

        entity.ToTable("order_lines");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.OrderId, x.ProductId }).IsUnique();
        entity.Property(x => x.UnitPrice).HasConversion(money).HasColumnType("numeric(18,0)");
        entity.Ignore(x => x.LineTotal);
    }
}
