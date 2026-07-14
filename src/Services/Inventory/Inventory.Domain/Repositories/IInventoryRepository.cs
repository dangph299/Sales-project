namespace Inventory.Domain;

/// <summary>
/// Command-side repository for inventory item aggregates.
/// </summary>
public interface IInventoryRepository
{
    /// <summary>
    /// Loads one inventory item by product id.
    /// </summary>
    Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads inventory items for the provided product ids.
    /// </summary>
    Task<IReadOnlyCollection<InventoryItem>> GetByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new inventory item.
    /// </summary>
    void Add(InventoryItem item);
}
