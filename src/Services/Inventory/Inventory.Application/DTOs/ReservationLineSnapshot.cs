namespace Inventory.Application;

/// <summary>
/// Read model for a single line of a reservation.
/// </summary>
/// <param name="ProductId">
/// The unique identifier of the reserved product.
/// </param>
/// <param name="Sku">
/// The product's normalized SKU.
/// </param>
/// <param name="Quantity">
/// The reserved quantity.
/// </param>
public sealed record ReservationLineSnapshot(Guid ProductId, string Sku, int Quantity);
