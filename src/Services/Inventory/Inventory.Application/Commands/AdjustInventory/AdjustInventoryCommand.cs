namespace Inventory.Application;

/// <summary>
/// Command that manually adjusts available stock for a product.
/// </summary>
/// <param name="ProductId">Product identifier.</param>
/// <param name="Sku">Product SKU, used when creating the item.</param>
/// <param name="QuantityDelta">Signed quantity to add to available stock.</param>
/// <param name="Actor">User or system responsible for the adjustment.</param>
public sealed record AdjustInventoryCommand(Guid ProductId, string Sku, int QuantityDelta, string Actor) : ICommand<InventorySnapshot>;
