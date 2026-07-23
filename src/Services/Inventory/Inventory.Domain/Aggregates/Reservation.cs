namespace Inventory.Domain;

/// <summary>
/// Aggregate root recording that stock was reserved for a Sales order. Created when a
/// <see cref="InventoryItem.Reserve"/> succeeds for every requested line, and released when the
/// order is cancelled or its confirmation is rejected.
/// </summary>
public sealed class Reservation : AggregateRoot<Guid>
{
    private readonly List<ReservationLine> _lines = [];
    private Reservation() { }

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
    /// <param name="orderId">Sales order the reservation is for.</param>
    /// <param name="lines">Product/quantity lines to reserve. Must contain at least one line, all with positive quantities.</param>
    /// <returns>Newly created reservation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="lines"/> is empty or contains a non-positive quantity.</exception>
    public static Reservation Create(Guid orderId, long orderVersion, IEnumerable<ReservationRequestLine> lines)
    {
        var reservation = new Reservation { Id = Guid.NewGuid(), OrderId = orderId, CreatedAt = DateTimeOffset.UtcNow, LastOrderVersion = orderVersion };
        reservation.SetLines(lines);
        return reservation;
    }

    /// <summary>
    /// Records that a release arrived for an order that has no reservation yet, as a line-less
    /// <see cref="ReservationStatus.Released"/> tombstone carrying the release's order version. It lets a
    /// later, newer confirmation still reserve stock via <see cref="Reactivate"/>, while a delayed
    /// reserve carrying an older-or-equal version is ignored via <see cref="IsStale"/> — closing the
    /// release-before-reserve out-of-order gap without holding stock for the cancelled order.
    /// </summary>
    /// <param name="orderId">Sales order the release was for.</param>
    /// <param name="orderVersion">Sales order version carried by the release event.</param>
    /// <returns>A released, line-less reservation acting as a staleness tombstone.</returns>
    public static Reservation CreateReleasedTombstone(Guid orderId, long orderVersion)
    {
        return new Reservation
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            CreatedAt = DateTimeOffset.UtcNow,
            LastOrderVersion = orderVersion,
            Status = ReservationStatus.Released
        };
    }

    /// <summary>
    /// Determines whether an order-version-carrying command is stale — i.e. carries a version no
    /// newer than the one already applied to this reservation — and should be ignored. This is the
    /// single staleness check used both to gate callers before they mutate other aggregates and by
    /// this aggregate's own state-changing methods, so the two can never disagree.
    /// </summary>
    /// <param name="orderVersion">Sales order version carried by the incoming command/event.</param>
    public bool IsStale(long orderVersion) => orderVersion <= LastOrderVersion;

    /// <summary>
    /// Replaces an active reservation with newer order lines.
    /// </summary>
    /// <param name="orderVersion">Sales order version carried by the confirmation event.</param>
    /// <param name="lines">Current product/quantity lines to reserve.</param>
    /// <returns><see langword="true"/> when this event advanced the reservation version; otherwise <see langword="false"/>.</returns>
    public bool ReplaceActive(long orderVersion, IEnumerable<ReservationRequestLine> lines)
    {
        if (Status != ReservationStatus.Active) throw new InvalidOperationException("Only active reservations can be replaced.");
        if (IsStale(orderVersion)) return false;
        var changed = LastOrderVersion != orderVersion;
        LastOrderVersion = orderVersion;
        changed = SetLines(lines) || changed;
        if (changed)
        {
            Touch();
        }

        return true;
    }

    /// <summary>
    /// Reactivates a previously released reservation with the current requested order lines.
    /// </summary>
    /// <param name="lines">Product/quantity lines to reserve. Must contain at least one line, all with positive quantities.</param>
    /// <exception cref="InvalidOperationException">Thrown when the reservation is not released, or when <paramref name="lines"/> is invalid.</exception>
    public bool Reactivate(long orderVersion, IEnumerable<ReservationRequestLine> lines)
    {
        if (Status != ReservationStatus.Released) throw new InvalidOperationException("Only released reservations can be reactivated.");
        if (IsStale(orderVersion)) return false;
        Status = ReservationStatus.Active;
        LastOrderVersion = orderVersion;
        SetLines(lines);
        Touch();
        return true;
    }

    /// <summary>
    /// Marks the reservation as released, so its stock can be returned to <see cref="InventoryItem.Available"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the reservation is not currently <see cref="ReservationStatus.Active"/>.</exception>
    public bool Release(long orderVersion)
    {
        if (Status != ReservationStatus.Active) throw new InvalidOperationException("Reservation is already released.");
        if (IsStale(orderVersion)) return false;
        LastOrderVersion = orderVersion;
        Status = ReservationStatus.Released;
        Touch();
        return true;
    }

    private bool SetLines(IEnumerable<ReservationRequestLine> lines)
    {
        var materialized = lines.ToArray();
        if (materialized.Length == 0) throw new InvalidOperationException("Reservation needs at least one line.");
        if (materialized.Any(x => x.Quantity <= 0)) throw new InvalidOperationException("Reservation quantity must be positive.");
        if (materialized.Select(x => x.ProductId).Distinct().Count() != materialized.Length)
            throw new InvalidOperationException("A product can occur only once in a reservation.");

        var changed = false;
        foreach (var existing in _lines.Where(existing => materialized.All(x => x.ProductId != existing.ProductId)).ToArray())
        {
            _lines.Remove(existing);
            changed = true;
        }

        foreach (var line in materialized)
        {
            var existing = _lines.SingleOrDefault(x => x.ProductId == line.ProductId);
            if (existing is null)
            {
                _lines.Add(ReservationLine.Create(Id, line));
                changed = true;
            }
            else if (existing.ReplaceWith(line))
            {
                changed = true;
            }
        }

        return changed;
    }
}
