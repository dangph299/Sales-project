namespace Inventory.Application;

/// <summary>
/// Read model for a single line of a reservation.
/// </summary>
/// <param name="ProductId">Product identifier.</param>
/// <param name="Sku">Product's normalized SKU.</param>
/// <param name="Quantity">Reserved quantity.</param>
public sealed record ReservationLineSnapshot(Guid ProductId, string Sku, int Quantity);
