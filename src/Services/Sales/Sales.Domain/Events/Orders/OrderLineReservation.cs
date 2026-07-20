namespace Sales.Domain;

/// <summary>
/// Describes the stock Inventory must reserve for one order line, as carried by
/// <see cref="OrderConfirmationRequestedDomainEvent"/>.
/// </summary>
/// <param name="ProductVariantId">Product variant to reserve.</param>
/// <param name="Sku">Product's SKU at the time the order line was created.</param>
/// <param name="Quantity">Quantity to reserve.</param>
public sealed record OrderLineReservation(Guid ProductVariantId, string Sku, int Quantity);
