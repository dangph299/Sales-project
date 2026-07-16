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
        return db.Inbox.AnyAsync(row => row.EventId == eventId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> TryRecordAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            db.Inbox.Add(InboxMessage.Create(eventId, clock.UtcNow, consumer: "inventory-v1"));
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
