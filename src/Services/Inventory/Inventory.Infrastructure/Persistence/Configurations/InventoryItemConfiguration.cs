using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Inventory.Domain;

namespace Inventory.Infrastructure;

/// <summary>
/// EF Core mapping for <see cref="InventoryItem"/>: table/index configuration, including the
/// unique SKU constraint and optimistic concurrency token.
/// </summary>
public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<InventoryItem> entity)
    {
        entity.ToTable("inventory_items").HasKey(x => x.ProductId);
        entity.Property(x => x.Version).IsConcurrencyToken();
        entity.HasIndex(x => x.Sku).IsUnique();
    }
}
