namespace Inventory.Domain;

/// <summary>
/// Aggregate root recording that stock was reserved for a Sales order. Created when a
/// <see cref="InventoryItem.Reserve"/> succeeds for every requested line, and released when the
/// order is cancelled or its confirmation is rejected.
/// </summary>
public sealed class Reservation
{
    private readonly List<ReservationLine> _lines = [];
    private Reservation() { }

    /// <summary>
    /// Gets the unique identifier of this reservation.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the Sales order this reservation was made for.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// Gets the reservation's current status.
    /// </summary>
    public ReservationStatus Status { get; private set; } = ReservationStatus.Active;

    /// <summary>
    /// Gets the UTC instant the reservation was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the reservation's lines.
    /// </summary>
    public IReadOnlyCollection<ReservationLine> Lines => _lines.AsReadOnly();

    /// <summary>
    /// Creates a new active reservation for an order.
    /// </summary>
    /// <param name="orderId">
    /// The unique identifier of the Sales order the reservation is for.
    /// </param>
    /// <param name="lines">
    /// The product/quantity lines to reserve. Must contain at least one line, all with positive quantities.
    /// </param>
    /// <returns>
    /// The newly created reservation.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="lines"/> is empty or contains a non-positive quantity.
    /// </exception>
    public static Reservation Create(Guid orderId, IEnumerable<ReservationRequestLine> lines)
    {
        var reservation = new Reservation { Id = Guid.NewGuid(), OrderId = orderId, CreatedAt = DateTimeOffset.UtcNow };
        var materialized = lines.ToArray();
        if (materialized.Length == 0) throw new InvalidOperationException("Reservation needs at least one line.");
        if (materialized.Any(x => x.Quantity <= 0)) throw new InvalidOperationException("Reservation quantity must be positive.");
        reservation._lines.AddRange(materialized.Select(x => ReservationLine.Create(reservation.Id, x)));
        return reservation;
    }

    /// <summary>
    /// Marks the reservation as released, so its stock can be returned to <see cref="InventoryItem.Available"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the reservation is not currently <see cref="ReservationStatus.Active"/>.
    /// </exception>
    public void Release()
    {
        if (Status != ReservationStatus.Active) throw new InvalidOperationException("Reservation is already released.");
        Status = ReservationStatus.Released;
    }
}
