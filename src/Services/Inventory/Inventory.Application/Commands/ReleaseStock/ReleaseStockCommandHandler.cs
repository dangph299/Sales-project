using MediatR;

namespace Inventory.Application;

/// <summary>
/// Handles stock release requests from Sales. Transaction management, inbox deduplication, and
/// commit are handled by <see cref="InventoryTransactionBehavior{TRequest,TResponse}"/>.
/// </summary>
public sealed class ReleaseStockCommandHandler(
    IInventoryRepository inventoryRepository,
    IReservationRepository reservationRepository,
    IInventoryEventOutbox inventoryEventOutbox) : IRequestHandler<ReleaseStockCommand, string>
{
    /// <inheritdoc/>
    public async Task<string> Handle(ReleaseStockCommand request, CancellationToken cancellationToken)
    {
        var reservation = await reservationRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (reservation is null)
        {
            // Release arrived before Reserve (out-of-order across the confirmation/undo topics). Record a
            // version-carrying tombstone so a delayed, older Reserve cannot silently hold stock for this
            // already-cancelled order. No stock is held yet, so nothing needs to be released here.
            reservationRepository.Add(Reservation.CreateReleasedTombstone(request.OrderId, request.OrderVersion));
            return "ReleasedBeforeReserve";
        }

        if (reservation.Status == ReservationStatus.Released) return "AlreadyReleased";
        if (reservation.IsStale(request.OrderVersion)) return "StaleRelease";

        var inventoryItems = await inventoryRepository.GetByProductIdsAsync(
            reservation.Lines.Select(x => x.ProductId),
            cancellationToken);
        foreach (var line in reservation.Lines)
        {
            inventoryItems.Single(x => x.ProductId == line.ProductId).Release(line.Quantity);
        }

        reservation.Release(request.OrderVersion);
        inventoryEventOutbox.EnqueueStockReleased(request.OrderId, request.OrderVersion, request.CorrelationId, request.EventId);
        return "Released";
    }
}
