namespace Inventory.Application;

/// <summary>
/// Mapping helpers for Inventory read models.
/// </summary>
public static class InventoryMappings
{
    /// <summary>
    /// Maps an inventory item to its snapshot DTO.
    /// </summary>
    public static InventorySnapshot ToSnapshot(this InventoryItem item)
    {
        return new InventorySnapshot(item.ProductId, item.Sku, item.Available, item.Reserved, item.Version);
    }
}
