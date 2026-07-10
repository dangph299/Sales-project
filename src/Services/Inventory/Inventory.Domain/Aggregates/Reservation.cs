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
    /// Gets the latest Sales order version applied to this reservation. Used to ignore delayed
    /// integration events that arrive out of order across Kafka topics.
    /// </summary>
    public long LastOrderVersion { get; private set; }

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
    public static Reservation Create(Guid orderId, long orderVersion, IEnumerable<ReservationRequestLine> lines)
    {
        var reservation = new Reservation { Id = Guid.NewGuid(), OrderId = orderId, CreatedAt = DateTimeOffset.UtcNow, LastOrderVersion = orderVersion };
        reservation.SetLines(lines);
        return reservation;
    }

    /// <summary>
    /// Records a newer confirmation event while the reservation is already active. This can happen
    /// when a reconfirm event overtakes a delayed release event from an earlier order version.
    /// </summary>
    /// <param name="orderVersion">
    /// The Sales order version carried by the confirmation event.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when this event advanced the reservation version; otherwise
    /// <see langword="false"/> because it was duplicate or stale.
    /// </returns>
    public bool AcknowledgeActive(long orderVersion)
    {
        if (Status != ReservationStatus.Active) throw new InvalidOperationException("Only active reservations can be acknowledged.");
        if (orderVersion <= LastOrderVersion) return false;
        LastOrderVersion = orderVersion;
        return true;
    }

    /// <summary>
    /// Reactivates a previously released reservation with the current requested order lines.
    /// </summary>
    /// <param name="lines">
    /// The product/quantity lines to reserve. Must contain at least one line, all with positive quantities.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the reservation is not released, or when <paramref name="lines"/> is invalid.
    /// </exception>
    public bool Reactivate(long orderVersion, IEnumerable<ReservationRequestLine> lines)
    {
        if (Status != ReservationStatus.Released) throw new InvalidOperationException("Only released reservations can be reactivated.");
        if (orderVersion <= LastOrderVersion) return false;
        Status = ReservationStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
        LastOrderVersion = orderVersion;
        SetLines(lines);
        return true;
    }

    /// <summary>
    /// Marks the reservation as released, so its stock can be returned to <see cref="InventoryItem.Available"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the reservation is not currently <see cref="ReservationStatus.Active"/>.
    /// </exception>
    public bool Release(long orderVersion)
    {
        if (Status != ReservationStatus.Active) throw new InvalidOperationException("Reservation is already released.");
        if (orderVersion <= LastOrderVersion) return false;
        LastOrderVersion = orderVersion;
        Status = ReservationStatus.Released;
        return true;
    }

    private void SetLines(IEnumerable<ReservationRequestLine> lines)
    {
        var materialized = lines.ToArray();
        if (materialized.Length == 0) throw new InvalidOperationException("Reservation needs at least one line.");
        if (materialized.Any(x => x.Quantity <= 0)) throw new InvalidOperationException("Reservation quantity must be positive.");
        if (materialized.Select(x => x.ProductId).Distinct().Count() != materialized.Length)
            throw new InvalidOperationException("A product can occur only once in a reservation.");

        foreach (var existing in _lines.Where(existing => materialized.All(x => x.ProductId != existing.ProductId)).ToArray())
        {
            _lines.Remove(existing);
        }

        foreach (var line in materialized)
        {
            var existing = _lines.SingleOrDefault(x => x.ProductId == line.ProductId);
            if (existing is null) _lines.Add(ReservationLine.Create(Id, line));
            else existing.ReplaceWith(line);
        }
    }
}
