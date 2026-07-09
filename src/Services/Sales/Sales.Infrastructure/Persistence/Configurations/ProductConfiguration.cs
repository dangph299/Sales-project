using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// EF Core mapping for <see cref="Product"/>: table/index/column configuration, including the
/// <see cref="Money"/> value converter and the unique SKU constraint, plus a trigram index on
/// <c>Name</c> for fast substring search.
/// </summary>
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        var money = new ValueConverter<Money, decimal>(x => x.Amount, x => Money.Vnd(x));

        entity.ToTable("products");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.Sku).IsUnique();
        entity.HasIndex(x => x.Name).HasMethod("gin").HasOperators("gin_trgm_ops");
        entity.Property(x => x.Name).HasMaxLength(200);
        entity.Property(x => x.Sku).HasMaxLength(64);
        entity.Property(x => x.Price).HasConversion(money).HasColumnType("numeric(18,0)");
        entity.Property(x => x.Version).IsConcurrencyToken();
    }
}
