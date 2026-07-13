using System.Diagnostics;
using System.Text.Json;
using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using KafkaFlow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Sales.Infrastructure;

/// <summary>
/// Kafka consumer handler for Inventory's integration events (<c>StockReserved</c>,
/// <c>StockRejected</c>, <c>StockReleased</c>), applying the corresponding status transition to the
/// matching <see cref="Sales.Domain.Order"/> with Inbox-based idempotency.
/// </summary>
/// <param name="scopes">Scope factory for per-message dependencies.</param>
/// <param name="logger">Logger used to record structured entries for each consumed message.</param>
/// <param name="activitySource">The <see cref="ActivitySource"/> used to start the tracing span for each consumed message.</param>
public sealed class SalesIntegrationEventHandler(
    IServiceScopeFactory scopes,
    ILogger<SalesIntegrationEventHandler> logger,
    ActivitySource activitySource) : IMessageHandler<EventEnvelope>
{
    /// <summary>
    /// Handles a single consumed message: opens a tracing span linked to the producer's trace,
    /// records the event in the Inbox for idempotency, and applies the corresponding order transition.
    /// </summary>
    /// <param name="context">KafkaFlow message context, providing topic/partition/offset/headers.</param>
    /// <param name="envelope">Deserialized event envelope.</param>
    public async Task Handle(IMessageContext context, EventEnvelope envelope)
    {
        using var activity = KafkaConsumerActivity.Start(activitySource, context);

        using (LogContext.PushProperty("EventId", envelope.EventId))
        using (LogContext.PushProperty("EventType", envelope.EventType))
        using (LogContext.PushProperty("CorrelationId", envelope.CorrelationId))
        using (LogContext.PushProperty("TraceId", activity?.TraceId.ToHexString()))
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await HandleCore(envelope);
                logger.LogInformation(
                    "Consumed {EventType} {Topic} {GroupId} {Partition} {Offset} {MessageId} {OrderId} {Result} {ElapsedMs}",
                    envelope.EventType, context.ConsumerContext.Topic, context.ConsumerContext.GroupId,
                    context.ConsumerContext.Partition, context.ConsumerContext.Offset, envelope.EventId,
                    envelope.AggregateId, result, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Consume failed {EventType} {Topic} {GroupId} {Partition} {Offset} {MessageId} {ElapsedMs}",
                    envelope.EventType, context.ConsumerContext.Topic, context.ConsumerContext.GroupId,
                    context.ConsumerContext.Partition, context.ConsumerContext.Offset, envelope.EventId, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }

    private async Task<string> HandleCore(EventEnvelope envelope)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            db.InboxMessages.Add(new InboxMessage { EventId = envelope.EventId, ProcessedAt = DateTimeOffset.UtcNow, Consumer = "sales-v1" });
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
            await transaction.CommitAsync();
            return "OrderNotFound";
        }

        string result;
        switch (envelope.EventType)
        {
            case nameof(StockReserved):
                order.MarkReserved();
                result = "Reserved";
                break;
            case nameof(StockRejected):
                order.RejectInventory(envelope.Data.Deserialize<StockRejected>()!.Reason);
                result = "Rejected";
                break;
            case nameof(StockReleased):
                result = "Released";
                break;
            default:
                await transaction.CommitAsync();
                return "Ignored";
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        SalesMetrics.InboxProcessed.Add(1);
        return result;
    }
}
