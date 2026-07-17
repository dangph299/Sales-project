using Inventory.Application.Common.Behaviors;
using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.Features.Reservations.Commands;

/// <summary>
/// Handles stock reservation requests from Sales. Transaction management, inbox deduplication,
/// and commit are handled by <see cref="InventoryTransactionBehavior{TRequest,TResponse}"/>.
/// </summary>
public sealed class ReserveStockCommandHandler(
    IInventoryRepository inventoryRepository,
    IReservationRepository reservationRepository,
    IInventoryEventOutbox inventoryEventOutbox,
    IInventoryMetrics metrics) : IRequestHandler<ReserveStockCommand, string>
{
    /// <inheritdoc/>
    public async Task<string> Handle(ReserveStockCommand request, CancellationToken cancellationToken)
    {
        var existingReservation = await reservationRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
        var requestedLines = request.Lines.Select(x => new ReservationRequestLine(x.ProductId, x.Sku, x.Quantity)).ToArray();

        if (existingReservation?.Status == ReservationStatus.Active)
        {
            return await ReplaceActiveReservation(request, existingReservation, requestedLines, cancellationToken);
        }

        if (existingReservation is not null && existingReservation.IsStale(request.OrderVersion))
        {
            return ErrorCodes.StaleReservation;
        }

        var items = await LoadItems(request.Lines.Select(x => x.ProductId), cancellationToken);
        var rejectedLine = FindRejectedLine(request.Lines, items);
        if (rejectedLine is not null)
        {
            EnqueueStockRejected(request, rejectedLine.Sku);
            metrics.RecordReservationRejected();
            return "Rejected";
        }

        foreach (var line in request.Lines)
        {
            items.Single(x => x.ProductId == line.ProductId).Reserve(line.Quantity);
        }

        if (existingReservation is null)
        {
            reservationRepository.Add(Reservation.Create(request.OrderId, request.OrderVersion, requestedLines));
        }
        else
        {
            // Staleness was already ruled out above using the same Reservation.IsStale check
            // Reactivate itself relies on, so this can only fail if Status is no longer Released
            // — not reachable here since the earlier branch already routed Active reservations
            // into ReplaceActiveReservation.
            if (!existingReservation.Reactivate(request.OrderVersion, requestedLines)) return ErrorCodes.StaleReservation;
        }

        inventoryEventOutbox.EnqueueStockReserved(request.OrderId, request.OrderVersion, request.CorrelationId, request.EventId);
        metrics.RecordReservationReserved();
        return "Reserved";
    }

    private async Task<string> ReplaceActiveReservation(
        ReserveStockCommand request,
        Reservation existingReservation,
        IReadOnlyCollection<ReservationRequestLine> requestedLines,
        CancellationToken cancellationToken)
    {
        if (existingReservation.IsStale(request.OrderVersion)) return "AlreadyReserved";

        var productIds = existingReservation.Lines.Select(x => x.ProductId)
            .Concat(requestedLines.Select(x => x.ProductId));
        var items = await LoadItems(productIds, cancellationToken);
        var rejectedLine = FindRejectedLineAfterReplacing(existingReservation, requestedLines, items);
        if (rejectedLine is not null)
        {
            EnqueueStockRejected(request, rejectedLine.Sku);
            metrics.RecordReservationRejected();
            return "Rejected";
        }

        foreach (var line in requestedLines)
        {
            var currentQuantity = existingReservation.Lines.SingleOrDefault(x => x.ProductId == line.ProductId)?.Quantity ?? 0;
            var delta = line.Quantity - currentQuantity;
            var item = items.Single(x => x.ProductId == line.ProductId);
            if (delta > 0) item.Reserve(delta);
            else if (delta < 0) item.Release(-delta);
        }

        foreach (var removed in existingReservation.Lines.Where(existing => requestedLines.All(x => x.ProductId != existing.ProductId)).ToArray())
        {
            items.Single(x => x.ProductId == removed.ProductId).Release(removed.Quantity);
        }

        // Staleness was already ruled out above using the same Reservation.IsStale check ReplaceActive
        // itself relies on, so this can only fail if Status is no longer Active — not reachable here
        // since the caller only routes into this method for an Active reservation.
        if (!existingReservation.ReplaceActive(request.OrderVersion, requestedLines)) return "AlreadyReserved";
        inventoryEventOutbox.EnqueueStockReserved(request.OrderId, request.OrderVersion, request.CorrelationId, request.EventId);
        metrics.RecordReservationReserved();
        return "ReservedAcknowledged";
    }

    private async Task<IReadOnlyCollection<InventoryItem>> LoadItems(IEnumerable<Guid> productIds, CancellationToken cancellationToken)
    {
        return await inventoryRepository.GetByProductIdsAsync(productIds, cancellationToken);
    }

    private static OrderLineIntegration? FindRejectedLine(IReadOnlyCollection<OrderLineIntegration> requestLines, IReadOnlyCollection<InventoryItem> items)
    {
        return requestLines.FirstOrDefault(line =>
            items.SingleOrDefault(x => x.ProductId == line.ProductId)?.Available < line.Quantity ||
            items.All(x => x.ProductId != line.ProductId));
    }

    private static ReservationRequestLine? FindRejectedLineAfterReplacing(
        Reservation reservation,
        IReadOnlyCollection<ReservationRequestLine> requestedLines,
        IReadOnlyCollection<InventoryItem> items)
    {
        return requestedLines.FirstOrDefault(line =>
        {
            var item = items.SingleOrDefault(x => x.ProductId == line.ProductId);
            if (item is null) return true;

            var currentlyReserved = reservation.Lines.SingleOrDefault(x => x.ProductId == line.ProductId)?.Quantity ?? 0;
            return item.Available + currentlyReserved < line.Quantity;
        });
    }

    private void EnqueueStockRejected(ReserveStockCommand request, string sku)
    {
        inventoryEventOutbox.EnqueueStockRejected(
            request.OrderId,
            request.OrderVersion,
            $"Insufficient stock for {sku}.",
            request.CorrelationId,
            request.EventId);
    }
}
