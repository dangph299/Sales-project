using System.Text.Json;
using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Sales.Infrastructure;

/// <summary>
/// Applies Inventory events to Sales orders, using Inbox idempotency.
/// </summary>
public sealed class SalesInventoryEventProcessor(
    SalesDbContext db,
    IClock clock,
    ILogger<SalesInventoryEventProcessor> logger) : IIntegrationEventProcessor
{
    /// <inheritdoc />
    public async Task<string> ProcessAsync(EventEnvelope envelope)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            // Insert Inbox first. A duplicate EventId means this Inventory reply was already applied.
            db.InboxMessages.Add(new InboxMessage { EventId = envelope.EventId, ProcessedAt = clock.UtcNow, Consumer = "sales-v1" });
            await db.SaveChangesAsync();
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
            // Commit the Inbox row so repeated delivery of this orphan event is still skipped.
            await transaction.CommitAsync();
            return ErrorCodes.OrderNotFound;
        }

        var result = ApplyOrderTransition(envelope, order);
        if (result == "Ignored")
        {
            // Unknown event type was recorded in Inbox but does not change Sales state.
            await transaction.CommitAsync();
            return result;
        }

        // Save the order status transition and Inbox row in one transaction.
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        SalesMetrics.InboxProcessed.Add(1);
        return result;
    }

    private static string ApplyOrderTransition(EventEnvelope envelope, Sales.Domain.Order order)
    {
        switch (envelope.EventType)
        {
            case nameof(StockReserved):
                order.MarkReserved();
                return "Reserved";
            case nameof(StockRejected):
                order.RejectInventory(envelope.Data.Deserialize<StockRejected>()!.Reason);
                return "Rejected";
            case nameof(StockReleased):
                return "Released";
            default:
                return "Ignored";
        }
    }
}
