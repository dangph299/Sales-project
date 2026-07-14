using MediatR;

namespace Inventory.Application;

/// <summary>
/// Handles stock release requests from Sales. Transaction management, inbox deduplication, and
/// commit are handled by <see cref="InventoryTransactionBehavior{TRequest,TResponse}"/>.
/// </summary>
public sealed class ReleaseStockCommandHandler(
    IInventoryRepository inventory,
    IReservationRepository reservations,
    IInventoryEventOutbox outbox) : IRequestHandler<ReleaseStockCommand, string>
{
    /// <inheritdoc/>
    public async Task<string> Handle(ReleaseStockCommand request, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (reservation is null || reservation.Status == ReservationStatus.Released) return "AlreadyReleased";
        if (reservation.IsStale(request.OrderVersion)) return "StaleRelease";

        var items = await inventory.GetByProductIdsAsync(reservation.Lines.Select(x => x.ProductId), cancellationToken);
        foreach (var line in reservation.Lines)
        {
            items.Single(x => x.ProductId == line.ProductId).Release(line.Quantity);
        }

        reservation.Release(request.OrderVersion);
        outbox.EnqueueStockReleased(request.OrderId, request.OrderVersion, request.CorrelationId, request.EventId);
        return "Released";
    }
}
