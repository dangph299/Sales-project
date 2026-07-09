namespace Inventory.Application;

/// <summary>
/// Read model for a reservation.
/// </summary>
/// <param name="OrderId">
/// The unique identifier of the Sales order the reservation was made for.
/// </param>
/// <param name="Status">
/// The reservation's current status, as its <c>ToString()</c> representation.
/// </param>
/// <param name="CreatedAt">
/// The UTC instant the reservation was created.
/// </param>
/// <param name="Lines">
/// The reservation's lines.
/// </param>
public sealed record ReservationSnapshot(Guid OrderId, string Status, DateTimeOffset CreatedAt, IReadOnlyCollection<ReservationLineSnapshot> Lines);
