namespace Inventory.Application.Features.Reservations.DTOs;

/// <summary>
/// Read model for a reservation.
/// </summary>
/// <param name="OrderId">Sales order the reservation was made for.</param>
/// <param name="Status">Reservation's current status, as its <c>ToString()</c> representation.</param>
/// <param name="CreatedAt">UTC instant the reservation was created.</param>
/// <param name="Lines">Reservation's lines.</param>
public sealed record ReservationSnapshot(Guid OrderId, string Status, DateTimeOffset CreatedAt, IReadOnlyCollection<ReservationLineSnapshot> Lines);
