namespace Inventory.Application;

/// <summary>
/// Query for the reservation associated with a Sales order.
/// </summary>
/// <param name="OrderId">Sales order identifier.</param>
public sealed record GetReservationByOrderQuery(Guid OrderId) : IQuery<ReservationSnapshot?>;
