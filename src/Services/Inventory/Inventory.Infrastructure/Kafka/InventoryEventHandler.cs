using System.Data;
using System.Diagnostics;
using System.Text.Json;
using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using Inventory.Domain;
using KafkaFlow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Inventory.Infrastructure;

/// <summary>
/// Kafka consumer handler for Sales' order integration events (<c>OrderConfirmationRequested</c>,
/// <c>OrderCancellationRequested</c>), reserving or releasing stock accordingly with Inbox-based
/// idempotency and a Serializable transaction to guard the stock invariant under concurrency.
/// </summary>
/// <param name="scopes">
/// The scope factory used to resolve per-message scoped dependencies such as the database context.
/// </param>
/// <param name="logger">
/// The logger used to record structured entries for each consumed message.
/// </param>
/// <param name="activitySource">
/// The <see cref="ActivitySource"/> used to start the tracing span for each consumed message.
/// </param>
public sealed class InventoryEventHandler(
    IServiceScopeFactory scopes,
    ILogger<InventoryEventHandler> logger,
    ActivitySource activitySource) : IMessageHandler<EventEnvelope>
{
    /// <summary>
    /// Handles a single consumed message: opens a tracing span linked to the producer's trace,
    /// records the event in the Inbox for idempotency, and reserves or releases stock accordingly.
    /// </summary>
    /// <param name="context">
    /// The KafkaFlow message context, providing topic/partition/offset/headers.
    /// </param>
    /// <param name="envelope">
    /// The deserialized event envelope.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
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
                var outcome = await HandleCore(envelope);
                logger.LogInformation(
                    "Consumed {EventType} {Topic} {GroupId} {Partition} {Offset} {MessageId} {AggregateId} {Result} {ElapsedMs}",
                    envelope.EventType, context.ConsumerContext.Topic, context.ConsumerContext.GroupId,
                    context.ConsumerContext.Partition, context.ConsumerContext.Offset, envelope.EventId,
                    envelope.AggregateId, outcome, sw.ElapsedMilliseconds);
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
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            db.Inbox.Add(new InboxRow { EventId = envelope.EventId, ProcessedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (PostgresExceptions.IsUniqueViolation(ex))
        {
            InventoryMetrics.InboxDuplicate.Add(1);
            await transaction.RollbackAsync();
            logger.LogDebug("Duplicate event skipped {EventId}", envelope.EventId);
            return "Duplicate";
        }

        string result;
        if (envelope.EventType == nameof(OrderConfirmationRequested)) result = await Reserve(db, envelope);
        else if (envelope.EventType == nameof(OrderCancellationRequested)) result = await Release(db, envelope);
        else
        {
            await transaction.CommitAsync();
            return "Ignored";
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        InventoryMetrics.InboxProcessed.Add(1);
        return result;
    }

    private static async Task<string> Reserve(InventoryDbContext db, EventEnvelope envelope)
    {
        var existingReservation = await db.Reservations
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.OrderId == envelope.AggregateId);

        var request = envelope.Data.Deserialize<OrderConfirmationRequested>()!;
        if (existingReservation?.Status == ReservationStatus.Active)
        {
            if (!existingReservation.AcknowledgeActive(envelope.Version)) return "AlreadyReserved";

            db.Enqueue(EventEnvelopeFactory.Create(request.OrderId, envelope.Version, new StockReserved(request.OrderId), "inventory", envelope.CorrelationId, envelope.EventId), KafkaTopics.StockReserved);
            return "ReservedAcknowledged";
        }

        var ids = request.Lines.Select(x => x.ProductId).Order().ToArray();
        var items = await db.Items.Where(x => ids.Contains(x.ProductId)).OrderBy(x => x.ProductId).ToListAsync();
        var error = request.Lines.FirstOrDefault(line => items.SingleOrDefault(x => x.ProductId == line.ProductId)?.Available < line.Quantity || items.All(x => x.ProductId != line.ProductId));
        if (error is not null)
        {
            db.Enqueue(EventEnvelopeFactory.Create(request.OrderId, envelope.Version,
                new StockRejected(request.OrderId, $"Insufficient stock for {error.Sku}."), "inventory", envelope.CorrelationId, envelope.EventId),
                KafkaTopics.StockRejected);
            InventoryMetrics.ReservationRejected.Add(1);
            return "Rejected";
        }
        foreach (var line in request.Lines) items.Single(x => x.ProductId == line.ProductId).Reserve(line.Quantity);
        var reservationLines = request.Lines.Select(x => new ReservationRequestLine(x.ProductId, x.Sku, x.Quantity));
        if (existingReservation is null)
        {
            db.Reservations.Add(Reservation.Create(request.OrderId, envelope.Version, reservationLines));
        }
        else
        {
            if (!existingReservation.Reactivate(envelope.Version, reservationLines)) return "StaleReservation";
        }

        db.Enqueue(EventEnvelopeFactory.Create(request.OrderId, envelope.Version, new StockReserved(request.OrderId), "inventory", envelope.CorrelationId, envelope.EventId), KafkaTopics.StockReserved);
        InventoryMetrics.ReservationReserved.Add(1);
        return "Reserved";
    }

    private static async Task<string> Release(InventoryDbContext db, EventEnvelope envelope)
    {
        var reservation = await db.Reservations.Include(x => x.Lines).SingleOrDefaultAsync(x => x.OrderId == envelope.AggregateId);
        if (reservation is null || reservation.Status == ReservationStatus.Released) return "AlreadyReleased";
        if (envelope.Version <= reservation.LastOrderVersion) return "StaleRelease";
        var ids = reservation.Lines.Select(x => x.ProductId).ToArray();
        var items = await db.Items.Where(x => ids.Contains(x.ProductId)).ToListAsync();
        foreach (var line in reservation.Lines) items.Single(x => x.ProductId == line.ProductId).Release(line.Quantity);
        reservation.Release(envelope.Version);
        db.Enqueue(EventEnvelopeFactory.Create(envelope.AggregateId, envelope.Version, new StockReleased(envelope.AggregateId), "inventory", envelope.CorrelationId, envelope.EventId), KafkaTopics.StockReleased);
        return "Released";
    }
}
