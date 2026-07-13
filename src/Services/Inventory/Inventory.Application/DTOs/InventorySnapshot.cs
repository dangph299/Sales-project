namespace Inventory.Application;

/// <summary>
/// Read model for a product's current stock levels.
/// </summary>
/// <param name="ProductId">Product identifier.</param>
/// <param name="Sku">Product's normalized SKU.</param>
/// <param name="Available">Quantity currently available to reserve.</param>
/// <param name="Reserved">Quantity currently reserved against active reservations.</param>
/// <param name="Version">Item's current optimistic concurrency version.</param>
public sealed record InventorySnapshot(Guid ProductId, string Sku, int Available, int Reserved, long Version);
