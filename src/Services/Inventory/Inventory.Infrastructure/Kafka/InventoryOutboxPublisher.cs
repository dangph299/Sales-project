using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

/// <summary>Publishes Inventory outbox rows to Kafka.</summary>
public sealed class InventoryOutboxPublisher(
    IServiceScopeFactory scopes,
    IOutboxPublisher publisher,
    ILogger<InventoryOutboxPublisher> logger,
    IClock clock,
    IOutboxSignal signal,
    IConfiguration configuration) : OutboxPublisherService<InventoryDbContext>(scopes, publisher, logger, signal, ReadPollInterval(configuration), () => clock.UtcNow)
{
    /// <inheritdoc />
    protected override DbSet<OutboxMessage> Outbox(InventoryDbContext db) => db.Outbox;

    /// <inheritdoc />
    protected override void RecordPublished() => InventoryMetrics.OutboxPublished.Add(1);

    /// <inheritdoc />
    protected override void RecordFailed() => InventoryMetrics.OutboxFailed.Add(1);

    /// <inheritdoc />
    protected override void RecordDeadLettered() => InventoryMetrics.OutboxDeadLettered.Add(1);

    /// <inheritdoc />
    protected override void SetSnapshot(long backlog, long deadLetters) => InventoryMetrics.SetOutboxSnapshot(backlog, deadLetters);
}
