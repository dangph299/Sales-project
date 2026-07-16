using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sales.Infrastructure;

/// <summary>Re-drives failed inbound Sales integration events with exponential backoff.</summary>
public sealed class SalesInboxRedriveService(
    IServiceScopeFactory scopes,
    ILogger<SalesInboxRedriveService> logger,
    IClock clock,
    IOptions<InboxConsumerOptions> options)
    : InboxRedriveService<SalesDbContext>(
        scopes,
        logger,
        options.Value.RedrivePollInterval,
        options.Value.RedriveBatchSize,
        () => clock.UtcNow)
{
    /// <inheritdoc/>
    protected override DbSet<InboxMessage> Inbox(SalesDbContext db) => db.InboxMessages;

    /// <inheritdoc/>
    protected override void RecordRetried() => SalesMetrics.InboxRetried.Add(1);

    /// <inheritdoc/>
    protected override void RecordDeadLettered() => SalesMetrics.InboxDeadLettered.Add(1);
}
