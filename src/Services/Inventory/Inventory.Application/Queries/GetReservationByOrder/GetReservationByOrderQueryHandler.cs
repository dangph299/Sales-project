using MediatR;

namespace Inventory.Application;

/// <summary>
/// Handles reservation lookups by Sales order.
/// </summary>
public sealed class GetReservationByOrderQueryHandler(IInventoryReadService readService)
    : IRequestHandler<GetReservationByOrderQuery, ReservationSnapshot?>
{
    /// <inheritdoc/>
    public Task<ReservationSnapshot?> Handle(GetReservationByOrderQuery request, CancellationToken cancellationToken)
    {
        return readService.GetReservationAsync(request.OrderId, cancellationToken);
    }
}
