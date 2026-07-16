using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inventory.Infrastructure;

/// <summary>Re-drives failed inbound Inventory integration events with exponential backoff.</summary>
public sealed class InventoryInboxRedriveService(
    IServiceScopeFactory scopes,
    ILogger<InventoryInboxRedriveService> logger,
    IClock clock,
    IOptions<InboxConsumerOptions> options)
    : InboxRedriveService<InventoryDbContext>(
        scopes,
        logger,
        options.Value.RedrivePollInterval,
        options.Value.RedriveBatchSize,
        () => clock.UtcNow)
{
    /// <inheritdoc/>
    protected override DbSet<InboxMessage> Inbox(InventoryDbContext db) => db.Inbox;

    /// <inheritdoc/>
    protected override void RecordRetried() => InventoryMetrics.InboxRetried.Add(1);

    /// <inheritdoc/>
    protected override void RecordDeadLettered() => InventoryMetrics.InboxDeadLettered.Add(1);
}
