using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sales.Infrastructure;

/// <summary>Publishes Sales outbox rows to Kafka.</summary>
public sealed class SalesOutboxPublisher(
    IServiceScopeFactory scopes,
    IOutboxPublisher publisher,
    ILogger<SalesOutboxPublisher> logger,
    IClock clock,
    IOutboxSignal signal,
    IConfiguration configuration) : OutboxPublisherService<SalesDbContext>(scopes, publisher, logger, signal, ReadPollInterval(configuration), () => clock.UtcNow)
{
    /// <inheritdoc />
    protected override DbSet<OutboxMessage> Outbox(SalesDbContext db) => db.OutboxMessages;

    /// <inheritdoc />
    protected override void RecordPublished() => SalesMetrics.OutboxPublished.Add(1);

    /// <inheritdoc />
    protected override void RecordFailed() => SalesMetrics.OutboxFailed.Add(1);

    /// <inheritdoc />
    protected override void RecordDeadLettered() => SalesMetrics.OutboxDeadLettered.Add(1);

    /// <inheritdoc />
    protected override void SetSnapshot(long backlog, long deadLetters) => SalesMetrics.SetOutboxSnapshot(backlog, deadLetters);

    private static TimeSpan ReadPollInterval(IConfiguration configuration)
    {
        var milliseconds = configuration.GetValue("Outbox:PollIntervalMilliseconds", 2_000);
        return TimeSpan.FromMilliseconds(Math.Clamp(milliseconds, 100, 60_000));
    }
}
