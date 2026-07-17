using Inventory.Application.Features.Reservations.DTOs;
using Inventory.Application.Features.Reservations.Interfaces;
using MediatR;

namespace Inventory.Application.Features.Reservations.Queries;

/// <summary>
/// Handles reservation lookups by Sales order.
/// </summary>
public sealed class GetReservationByOrderQueryHandler(IReservationReadService readService)
    : IRequestHandler<GetReservationByOrderQuery, ReservationSnapshot?>
{
    /// <inheritdoc/>
    public Task<ReservationSnapshot?> Handle(GetReservationByOrderQuery request, CancellationToken cancellationToken)
    {
        return readService.GetReservationAsync(request.OrderId, cancellationToken);
    }
}
