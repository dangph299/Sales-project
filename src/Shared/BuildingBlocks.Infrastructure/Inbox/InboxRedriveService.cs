using System.Text.Json;
using BuildingBlocks.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Background service that re-drives inbound integration events whose first processing failed.
/// KafkaFlow commits the consumer offset even when a handler throws, so Kafka never redelivers a
/// failed message; this service is the retry mechanism. It replays each failed inbox row's stored
/// envelope through <see cref="IIntegrationEventProcessor"/> with exponential backoff until the event
/// succeeds or is dead-lettered. Replay is idempotent because the processor keys on the inbox
/// <c>EventId</c>.
/// </summary>
public abstract class InboxRedriveService<TDbContext>(
    IServiceScopeFactory scopes,
    ILogger logger,
    TimeSpan pollInterval,
    int batchSize,
    Func<DateTimeOffset> utcNow) : BackgroundService
    where TDbContext : DbContext
{
    /// <summary>Returns the service-owned inbox table.</summary>
    protected abstract DbSet<InboxMessage> Inbox(TDbContext db);

    /// <summary>Records a previously failed message re-driven to success in service metrics.</summary>
    protected abstract void RecordRetried();

    /// <summary>Records a re-driven message moved to dead-letter state in service metrics.</summary>
    protected abstract void RecordDeadLettered();

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RedriveDueMessages(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "{DbContext} inbox re-drive cycle failed", typeof(TDbContext).Name);
            }

            try
            {
                await Task.Delay(pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Runs exactly one re-drive cycle. Exposed to reliability tests so the retry and dead-letter
    /// state machine can be exercised deterministically without starting the background loop.
    /// </summary>
    internal Task RunRedriveCycleAsync(CancellationToken cancellationToken = default) => RedriveDueMessages(cancellationToken);

    private async Task RedriveDueMessages(CancellationToken cancellationToken)
    {
        var now = utcNow();
        List<InboxMessage> dueMessages;
        await using (var scope = scopes.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
            dueMessages = await Inbox(db)
                .AsNoTracking()
                .Where(x => x.Status == InboxMessageStatus.Failed
                    && x.Payload != null
                    && (x.NextAttemptAt == null || x.NextAttemptAt <= now))
                .OrderBy(x => x.NextAttemptAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
        }

        foreach (var message in dueMessages)
        {
            await RedriveOne(message, cancellationToken);
        }
    }

    private async Task RedriveOne(InboxMessage message, CancellationToken cancellationToken)
    {
        var envelope = JsonSerializer.Deserialize<EventEnvelope>(message.Payload!)!;
        try
        {
            // Process in its own scope so a failed attempt's tracked changes never leak into the
            // failure-recording scope below (mirrors IntegrationEventHandler's scoping). On success the
            // processor marks the inbox row Processed inside its own transaction.
            await using var processScope = scopes.CreateAsyncScope();
            var processor = processScope.ServiceProvider.GetRequiredService<IIntegrationEventProcessor>();
            await processor.ProcessAsync(envelope);
            RecordRetried();
            logger.LogInformation("Inbox re-drive succeeded {MessageId} {EventType}", envelope.EventId, envelope.EventType);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await using var failureScope = scopes.CreateAsyncScope();
            var failureRecorder = failureScope.ServiceProvider.GetRequiredService<IInboxFailureRecorder>();
            var failure = await failureRecorder.RecordFailureAsync(
                envelope,
                new InboundMessageContext(
                    message.OriginalTopic ?? string.Empty,
                    message.OriginalConsumerGroup ?? string.Empty,
                    message.OriginalPartition ?? 0,
                    message.OriginalOffset ?? 0),
                ex,
                cancellationToken);
            if (failure.DeadLettered)
            {
                RecordDeadLettered();
            }

            logger.LogWarning(ex,
                "Inbox re-drive failed {MessageId} {Attempts} {DeadLettered}",
                envelope.EventId, failure.Attempts, failure.DeadLettered);
        }
    }
}
