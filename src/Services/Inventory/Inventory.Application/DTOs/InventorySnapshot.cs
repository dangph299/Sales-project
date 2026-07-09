namespace Inventory.Application;

/// <summary>
/// Read model for a product's current stock levels.
/// </summary>
/// <param name="ProductId">
/// The unique identifier of the product.
/// </param>
/// <param name="Sku">
/// The product's normalized SKU.
/// </param>
/// <param name="Available">
/// The quantity currently available to reserve.
/// </param>
/// <param name="Reserved">
/// The quantity currently reserved against active reservations.
/// </param>
/// <param name="Version">
/// The item's current optimistic concurrency version.
/// </param>
public sealed record InventorySnapshot(Guid ProductId, string Sku, int Available, int Reserved, long Version);
