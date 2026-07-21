using System.Text.Json;
using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sales.Application.Features.Orders.Realtime;

namespace Sales.Infrastructure;

/// <summary>
/// Applies Inventory events to Sales orders, using Inbox idempotency.
/// </summary>
public sealed class SalesInventoryEventProcessor(
    SalesDbContext db,
    IClock clock,
    IOrderRealtimeNotifier orderRealtimeNotifier,
    ILogger<SalesInventoryEventProcessor> logger) : IIntegrationEventProcessor
{
    /// <inheritdoc />
    public async Task<string> ProcessAsync(EventEnvelope envelope)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            var inbox = await db.InboxMessages.SingleOrDefaultAsync(x => x.EventId == envelope.EventId);
            if (inbox is null)
            {
                // Insert Inbox first. A duplicate EventId means this Inventory reply was already applied.
                db.InboxMessages.Add(InboxMessage.Create(envelope.EventId, clock.UtcNow, consumer: "sales-v1"));
                await db.SaveChangesAsync();
            }
            else if (inbox.Status is InboxMessageStatus.Processed or InboxMessageStatus.DeadLettered)
            {
                SalesMetrics.InboxDuplicate.Add(1);
                await transaction.RollbackAsync();
                logger.LogDebug("Duplicate event skipped {EventId} {Status}", envelope.EventId, inbox.Status);
                return "Duplicate";
            }
            else
            {
                inbox.Status = InboxMessageStatus.Processed;
                inbox.ProcessedAt = clock.UtcNow;
                inbox.DeadLetteredAt = null;
            }
        }
        catch (DbUpdateException ex) when (PostgresExceptions.IsUniqueViolation(ex))
        {
            SalesMetrics.InboxDuplicate.Add(1);
            await transaction.RollbackAsync();
            logger.LogDebug("Duplicate event skipped {EventId}", envelope.EventId);
            return "Duplicate";
        }

        var order = await db.Orders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == envelope.AggregateId);
        if (order is null)
        {
            // Persist the Inbox row so repeated delivery of this orphan event is still skipped.
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return ErrorCodes.OrderNotFound;
        }

        var previousStatus = order.Status;
        var transition = ApplyOrderTransition(envelope, order);
        var currentStatus = order.Status;

        // Save unconditionally: the Order transition and the Inbox row's own processing state are two
        // separate changes, and only the first of them depends on the transition. An event Sales has
        // no handler for is still processed successfully, so its Inbox row must be persisted as
        // Processed here - a re-driven row left in Failed state is selected by InboxRedriveService on
        // every cycle forever, because the re-drive success path persists nothing itself.
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        await NotifyOrderStatusChangedAfterCommit(
            order,
            previousStatus,
            currentStatus);

        // Metric unchanged: it counts events that moved a Sales Order, not inbox rows retired.
        if (transition != OrderTransition.Ignored)
        {
            SalesMetrics.InboxProcessed.Add(1);
        }

        return transition.GetDescription();
    }

    private async Task NotifyOrderStatusChangedAfterCommit(
        Sales.Domain.Order order,
        Sales.Domain.OrderStatus previousStatus,
        Sales.Domain.OrderStatus currentStatus)
    {
        if (previousStatus == currentStatus)
        {
            return;
        }

        try
        {
            await orderRealtimeNotifier.NotifyOrderStatusChangedAsync(
                new OrderStatusChangedNotification(
                    order.Id,
                    previousStatus,
                    currentStatus,
                    clock.UtcNow,
                    order.Version),
                CancellationToken.None);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Order realtime notification failed after commit {OrderId} {PreviousStatus} {CurrentStatus}",
                order.Id,
                previousStatus,
                currentStatus);
        }
    }

    private static OrderTransition ApplyOrderTransition(EventEnvelope envelope, Sales.Domain.Order order)
    {
        switch (envelope.EventType)
        {
            case nameof(StockReserved):
                order.MarkReserved();
                return OrderTransition.Reserved;
            case nameof(StockRejected):
                order.RejectInventory(envelope.Data.Deserialize<StockRejected>()!.Reason);
                return OrderTransition.Rejected;
            case nameof(StockReleased):
                return OrderTransition.Released;
            default:
                return OrderTransition.Ignored;
        }
    }

    /// <summary>
    /// How a consumed Inventory event affected the Sales order. Only
    /// <see cref="OrderTransition.Ignored"/> leaves the order untouched.
    /// </summary>
    /// <remarks>
    /// Member names are already the outcome text reported to consume logs, so no
    /// <see cref="System.ComponentModel.DescriptionAttribute"/> is declared - <c>GetDescription()</c>
    /// falls back to the member name. Renaming a member therefore changes that log value;
    /// <c>SalesInventoryEventProcessorTests</c> asserts the exact strings and fails if one is renamed.
    /// </remarks>
    private enum OrderTransition
    {
        Ignored,
        Reserved,
        Rejected,
        Released
    }
}
