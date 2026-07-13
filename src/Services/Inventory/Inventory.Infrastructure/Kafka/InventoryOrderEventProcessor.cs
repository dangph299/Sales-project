using System.Data;
using System.Text.Json;
using BuildingBlocks.Application;
using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure;

/// <summary>
/// Applies Sales order events to Inventory, using Inbox idempotency and Outbox replies.
/// </summary>
public sealed class InventoryOrderEventProcessor(
    InventoryDbContext db,
    IClock clock,
    ILogger<InventoryOrderEventProcessor> logger) : IIntegrationEventProcessor
{
    /// <inheritdoc />
    public async Task<string> ProcessAsync(EventEnvelope envelope)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            // Insert Inbox first. A duplicate EventId means Kafka redelivered a message already handled.
            db.Inbox.Add(new InboxRow { EventId = envelope.EventId, ProcessedAt = clock.UtcNow });
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (PostgresExceptions.IsUniqueViolation(ex))
        {
            InventoryMetrics.InboxDuplicate.Add(1);
            await transaction.RollbackAsync();
            logger.LogDebug("Duplicate event skipped {EventId}", envelope.EventId);
            return "Duplicate";
        }

        var result = envelope.EventType switch
        {
            nameof(OrderConfirmationRequested) => await Reserve(envelope),
            nameof(OrderCancellationRequested) => await Release(envelope),
            _ => "Ignored"
        };

        if (result == "Ignored")
        {
            // Unknown event type was still recorded in Inbox, so commit and do no business changes.
            await transaction.CommitAsync();
            return result;
        }

        // Save stock/reservation changes together with any reply event enqueued to Inventory Outbox.
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        InventoryMetrics.InboxProcessed.Add(1);
        return result;
    }

    private async Task<string> Reserve(EventEnvelope envelope)
    {
        var request = envelope.Data.Deserialize<OrderConfirmationRequested>()!;
        var existingReservation = await db.Reservations
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.OrderId == envelope.AggregateId);

        if (existingReservation?.Status == ReservationStatus.Active)
        {
            // Re-acknowledge an active reservation without reserving stock twice.
            if (!existingReservation.AcknowledgeActive(envelope.Version)) return "AlreadyReserved";

            EnqueueStockReserved(request.OrderId, envelope);
            return "ReservedAcknowledged";
        }

        var items = await LoadItems(request.Lines.Select(x => x.ProductId));
        var rejectedLine = FindRejectedLine(request, items);
        if (rejectedLine is not null)
        {
            // Tell Sales to reject the order when any line cannot be reserved.
            EnqueueStockRejected(request.OrderId, envelope, rejectedLine.Sku);
            InventoryMetrics.ReservationRejected.Add(1);
            return "Rejected";
        }

        // Reserve stock before creating/reactivating the reservation record.
        foreach (var line in request.Lines)
        {
            items.Single(x => x.ProductId == line.ProductId).Reserve(line.Quantity);
        }

        var reservationLines = request.Lines.Select(x => new ReservationRequestLine(x.ProductId, x.Sku, x.Quantity));
        if (existingReservation is null)
        {
            db.Reservations.Add(Reservation.Create(request.OrderId, envelope.Version, reservationLines));
        }
        else if (!existingReservation.Reactivate(envelope.Version, reservationLines))
        {
            // Ignore older confirmation events that arrive after a newer release/reconfirm sequence.
            return "StaleReservation";
        }

        EnqueueStockReserved(request.OrderId, envelope);
        InventoryMetrics.ReservationReserved.Add(1);
        return "Reserved";
    }

    private async Task<string> Release(EventEnvelope envelope)
    {
        var reservation = await db.Reservations
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.OrderId == envelope.AggregateId);

        if (reservation is null || reservation.Status == ReservationStatus.Released) return "AlreadyReleased";
        if (envelope.Version <= reservation.LastOrderVersion) return "StaleRelease";

        // Put reserved quantities back into available stock.
        var items = await LoadItems(reservation.Lines.Select(x => x.ProductId));
        foreach (var line in reservation.Lines)
        {
            items.Single(x => x.ProductId == line.ProductId).Release(line.Quantity);
        }

        reservation.Release(envelope.Version);
        db.Enqueue(
            EventEnvelopeFactory.Create(envelope.AggregateId, envelope.Version, new StockReleased(envelope.AggregateId), "inventory", envelope.CorrelationId, envelope.EventId),
            KafkaTopics.StockReleased);
        return "Released";
    }

    private async Task<List<InventoryItem>> LoadItems(IEnumerable<Guid> productIds)
    {
        var ids = productIds.Order().ToArray();
        return await db.Items.Where(x => ids.Contains(x.ProductId)).OrderBy(x => x.ProductId).ToListAsync();
    }

    private static OrderLineIntegration? FindRejectedLine(OrderConfirmationRequested request, IReadOnlyCollection<InventoryItem> items)
    {
        return request.Lines.FirstOrDefault(line =>
            items.SingleOrDefault(x => x.ProductId == line.ProductId)?.Available < line.Quantity ||
            items.All(x => x.ProductId != line.ProductId));
    }

    private void EnqueueStockReserved(Guid orderId, EventEnvelope envelope)
    {
        db.Enqueue(
            EventEnvelopeFactory.Create(orderId, envelope.Version, new StockReserved(orderId), "inventory", envelope.CorrelationId, envelope.EventId),
            KafkaTopics.StockReserved);
    }

    private void EnqueueStockRejected(Guid orderId, EventEnvelope envelope, string sku)
    {
        db.Enqueue(
            EventEnvelopeFactory.Create(orderId, envelope.Version, new StockRejected(orderId, $"Insufficient stock for {sku}."), "inventory", envelope.CorrelationId, envelope.EventId),
            KafkaTopics.StockRejected);
    }
}
