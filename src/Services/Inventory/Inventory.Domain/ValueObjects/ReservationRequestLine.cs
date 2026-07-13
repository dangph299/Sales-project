namespace Inventory.Domain;

/// <summary>
/// A requested product/quantity to reserve, passed to <see cref="Reservation.Create"/>.
/// </summary>
/// <param name="ProductId">Product identifier.</param>
/// <param name="Sku">Product's SKU.</param>
/// <param name="Quantity">Requested quantity.</param>
public sealed record ReservationRequestLine(Guid ProductId, string Sku, int Quantity);
