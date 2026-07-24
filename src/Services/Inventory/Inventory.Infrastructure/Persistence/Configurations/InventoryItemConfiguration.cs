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
        // Table
        entity.ToTable("inventory_items");

        // Primary Key
        entity.HasKey(x => x.ProductVariantId);

        // Properties
        entity.Property(x => x.ProductVariantId).HasColumnName("ProductId").ValueGeneratedNever();
        entity.Property(x => x.CreatedAt);
        entity.Property(x => x.UpdatedAt);
        entity.Property(x => x.Version).IsConcurrencyToken();

        // Indexes
        entity.HasIndex(x => x.Sku).IsUnique();
    }
}
