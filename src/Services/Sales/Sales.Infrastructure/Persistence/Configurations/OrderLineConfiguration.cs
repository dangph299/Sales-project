using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sales.Domain;

namespace Sales.Infrastructure;

public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> entity)
    {
        var money = new ValueConverter<Money, decimal>(x => x.Amount, x => Money.Vnd(x));

        entity.ToTable("order_lines");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.HasIndex(x => new { x.OrderId, x.ProductVariantId }).IsUnique();
        entity.Property(x => x.ProductCode).HasMaxLength(32);
        entity.Property(x => x.Sku).HasMaxLength(96);
        entity.Property(x => x.ProductName).HasMaxLength(200);
        entity.Property(x => x.ColorCode).HasMaxLength(16);
        entity.Property(x => x.ColorName).HasMaxLength(100);
        entity.Property(x => x.SizeCode).HasMaxLength(16);
        entity.Property(x => x.UnitPrice).HasConversion(money).HasColumnType("numeric(18,0)");
        entity.Ignore(x => x.LineTotal);
    }
}
