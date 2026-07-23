using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Inventory.Domain;

namespace Inventory.Infrastructure;

/// <summary>
/// Persistence mapping for inventory items.
/// </summary>
public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<InventoryItem> entity)
    {
        entity.ToTable("inventory_items").HasKey(x => x.ProductVariantId);
        entity.Property(x => x.ProductVariantId).HasColumnName("ProductId").ValueGeneratedNever();
        entity.Property(x => x.CreatedAt);
        entity.Property(x => x.UpdatedAt);
        entity.Property(x => x.Version).IsConcurrencyToken();
        entity.HasIndex(x => x.Sku).IsUnique();
    }
}
