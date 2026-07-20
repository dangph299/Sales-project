namespace BuildingBlocks.Contracts;

/// <summary>
/// Describes the stock Inventory must reserve for one order line, as carried by
/// <see cref="OrderConfirmationRequested"/>.
/// </summary>
/// <param name="ProductId">Product variant to reserve. Kept as ProductId for contract v1 compatibility.</param>
/// <param name="Sku">Product's SKU at the time the order line was created.</param>
/// <param name="Quantity">Quantity to reserve.</param>
public sealed record OrderLineIntegration(Guid ProductId, string Sku, int Quantity);
