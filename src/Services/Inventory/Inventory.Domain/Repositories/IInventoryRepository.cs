namespace Inventory.Domain;

/// <summary>
/// Command-side repository for inventory item aggregates.
/// </summary>
public interface IInventoryRepository
{
    /// <summary>
    /// Loads one inventory item by product variant id.
    /// </summary>
    Task<InventoryItem?> GetByProductVariantIdAsync(Guid productVariantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads inventory items for the provided product variant ids.
    /// </summary>
    Task<IReadOnlyCollection<InventoryItem>> GetByProductVariantIdsAsync(IEnumerable<Guid> productVariantIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new inventory item.
    /// </summary>
    void Add(InventoryItem item);
}
