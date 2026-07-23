namespace Inventory.Domain;

/// <summary>
/// A single product/quantity line owned by a <see cref="Reservation"/>.
/// </summary>
public sealed class ReservationLine : Entity<Guid>
{
    private ReservationLine() { }

    /// Gets the unique identifier of the <see cref="Reservation"/> that owns this line.
    /// </summary>
    public Guid ReservationId { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the reserved product.
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Gets the product's normalized SKU.
    /// </summary>
    public string Sku { get; private set; } = null!;

    /// <summary>
    /// Gets the reserved quantity.
    /// </summary>
    public int Quantity { get; private set; }

    internal static ReservationLine Create(Guid reservationId, ReservationRequestLine line)
    {
        return new ReservationLine
        {
            Id = Guid.NewGuid(),
            ReservationId = reservationId,
            ProductId = line.ProductId,
            Sku = line.Sku.Trim().ToUpperInvariant(),
            Quantity = line.Quantity
        };
    }

    internal bool ReplaceWith(ReservationRequestLine line)
    {
        if (line.ProductId != ProductId) throw new InvalidOperationException("Reservation line product cannot be changed.");
        var normalizedSku = line.Sku.Trim().ToUpperInvariant();
        if (Sku == normalizedSku && Quantity == line.Quantity)
        {
            return false;
        }

        Sku = normalizedSku;
        Quantity = line.Quantity;
        return true;
    }
}
