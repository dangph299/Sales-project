using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Persistence mapping for catalog products.
/// </summary>
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        var money = new ValueConverter<Money, decimal>(x => x.Amount, x => Money.Vnd(x));

        entity.ToTable("products");
        entity.HasKey(x => x.Id);
        entity.HasQueryFilter(x => !x.IsDelete);
        entity.HasIndex(x => x.Sku).IsUnique();
        entity.HasIndex(x => x.Name).HasMethod("gin").HasOperators("gin_trgm_ops");
        entity.Property(x => x.Name).HasMaxLength(200);
        entity.Property(x => x.Sku).HasMaxLength(64);
        entity.Property(x => x.DeleteByUser).HasMaxLength(128);
        entity.Property(x => x.Price).HasConversion(money).HasColumnType("numeric(18,0)");
        entity.Property(x => x.Version).IsConcurrencyToken();
    }
}
