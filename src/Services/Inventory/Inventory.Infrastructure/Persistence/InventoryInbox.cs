using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using Inventory.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure;

/// <summary>
/// EF Core inbox implementation for Inventory integration events.
/// </summary>
public sealed class InventoryInbox(
    InventoryDbContext db,
    IClock clock,
    ILogger<InventoryInbox> logger) : IInventoryInbox
{
    /// <inheritdoc/>
    public Task<bool> HasBeenProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return db.Inbox.AnyAsync(row =>
            row.EventId == eventId &&
            (row.Status == InboxMessageStatus.Processed || row.Status == InboxMessageStatus.DeadLettered),
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> TryRecordAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var row = await db.Inbox.SingleOrDefaultAsync(x => x.EventId == eventId, cancellationToken);
            if (row is null)
            {
                db.Inbox.Add(InboxMessage.Create(eventId, clock.UtcNow, consumer: "inventory-v1"));
            }
            else if (row.Status is InboxMessageStatus.Processed or InboxMessageStatus.DeadLettered)
            {
                return false;
            }
            else
            {
                row.Status = InboxMessageStatus.Processed;
                row.ProcessedAt = clock.UtcNow;
                row.DeadLetteredAt = null;
            }

            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (PostgresExceptions.IsUniqueViolation(ex))
        {
            logger.LogDebug("Duplicate event skipped {EventId}", eventId);
            return false;
        }
    }
}
