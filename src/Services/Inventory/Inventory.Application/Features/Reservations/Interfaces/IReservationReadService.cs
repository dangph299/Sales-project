using Inventory.Application.Features.Reservations.DTOs;

namespace Inventory.Application.Features.Reservations.Interfaces;

/// <summary>
/// Read-side port for reservation queries.
/// </summary>
public interface IReservationReadService
{
    /// <summary>
    /// Gets the reservation for a Sales order.
    /// </summary>
    Task<ReservationSnapshot?> GetReservationAsync(Guid orderId, CancellationToken cancellationToken = default);
}
