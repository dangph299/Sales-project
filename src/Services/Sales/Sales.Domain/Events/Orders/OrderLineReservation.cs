namespace Sales.Domain;

/// <summary>
/// Describes the stock Inventory must reserve for one order line, as carried by
/// <see cref="OrderConfirmationRequestedDomainEvent"/>.
/// </summary>
/// <param name="ProductId">
/// The unique identifier of the product to reserve.
/// </param>
/// <param name="Sku">
/// The product's SKU at the time the order line was created.
/// </param>
/// <param name="Quantity">
/// The quantity to reserve.
/// </param>
public sealed record OrderLineReservation(Guid ProductId, string Sku, int Quantity);
