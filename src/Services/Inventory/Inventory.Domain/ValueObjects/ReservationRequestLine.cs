namespace Inventory.Domain;

/// <summary>
/// A requested product/quantity to reserve, passed to <see cref="Reservation.Create"/>.
/// </summary>
/// <param name="ProductId">
/// The unique identifier of the requested product.
/// </param>
/// <param name="Sku">
/// The product's SKU.
/// </param>
/// <param name="Quantity">
/// The requested quantity.
/// </param>
public sealed record ReservationRequestLine(Guid ProductId, string Sku, int Quantity);
