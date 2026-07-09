namespace Inventory.Domain;

/// <summary>
/// A single product/quantity line owned by a <see cref="Reservation"/>.
/// </summary>
public sealed class ReservationLine
{
    private ReservationLine() { }

    /// <summary>
    /// Gets the unique identifier of this line.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
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

    /// <summary>
    /// Creates a new reservation line from a requested line.
    /// </summary>
    /// <param name="reservationId">
    /// The unique identifier of the owning reservation.
    /// </param>
    /// <param name="line">
    /// The requested product/quantity.
    /// </param>
    /// <returns>
    /// The new reservation line.
    /// </returns>
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

    internal void ReplaceWith(ReservationRequestLine line)
    {
        if (line.ProductId != ProductId) throw new InvalidOperationException("Reservation line product cannot be changed.");
        Sku = line.Sku.Trim().ToUpperInvariant();
        Quantity = line.Quantity;
    }
}
