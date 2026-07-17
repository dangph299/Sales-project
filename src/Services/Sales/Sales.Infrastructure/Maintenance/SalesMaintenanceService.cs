using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Sales.Infrastructure;

/// <summary>
/// Provides operator-facing recovery operations for Sales inbox and outbox messages. These are
/// triggered manually after an operator fix; none of them is a scheduled recurring job.
/// </summary>
public sealed class SalesMaintenanceService(SalesDbContext db, IClock clock)
{
    private const int ReplayBatchLimit = 100;

    /// <summary>
    /// Resets a single outbox message so the publisher will attempt to publish it again on its next cycle.
    /// </summary>
    /// <param name="outboxMessageId">Outbox message to replay.</param>
    /// <returns><see langword="true"/> if a matching outbox row was found and reset; otherwise <see langword="false"/>.</returns>
    public async Task<bool> ReplayOutboxMessageAsync(
        Guid outboxMessageId,
        CancellationToken cancellationToken = default)
    {
        var outboxMessage = await db.OutboxMessages.SingleOrDefaultAsync(
            row => row.Id == outboxMessageId,
            cancellationToken);
        if (outboxMessage is null)
        {
            return false;
        }

        ResetOutboxForReplay(outboxMessage);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Resets up to <paramref name="maximumMessageCount"/> dead-lettered outbox messages so the publisher
    /// will attempt to publish them again on its next cycle.
    /// </summary>
    /// <param name="maximumMessageCount">Maximum number of dead-lettered messages to reset. Clamped between 1 and 100.</param>
    /// <returns>Number of outbox rows that were reset.</returns>
    public async Task<int> ReplayDeadLetterOutboxMessagesAsync(
        int maximumMessageCount = ReplayBatchLimit,
        CancellationToken cancellationToken = default)
    {
        var batchSize = Math.Clamp(maximumMessageCount, 1, ReplayBatchLimit);
        var outboxMessages = await db.OutboxMessages
            .Where(row => row.ProcessedAt == null && row.DeadLetteredAt != null)
            .OrderBy(row => row.DeadLetteredAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var outboxMessage in outboxMessages)
        {
            ResetOutboxForReplay(outboxMessage);
        }

        await db.SaveChangesAsync(cancellationToken);
        return outboxMessages.Count;
    }

    /// <summary>
    /// Resets a single inbound dead-lettered message so a Kafka/DLQ replay can process it again.
    /// </summary>
    /// <param name="eventId">Inbound event id to reset.</param>
    /// <returns><see langword="true"/> if a matching dead-lettered inbox row was reset; otherwise <see langword="false"/>.</returns>
    public async Task<bool> ResetInboxDeadLetterAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var inboxMessage = await db.InboxMessages.SingleOrDefaultAsync(
            row => row.EventId == eventId && row.Status == InboxMessageStatus.DeadLettered,
            cancellationToken);
        if (inboxMessage is null)
        {
            return false;
        }

        ResetInboxForReplay(inboxMessage);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Resets up to <paramref name="maximumMessageCount"/> inbound dead-lettered messages so Kafka/DLQ
    /// replay can process them again.
    /// </summary>
    /// <param name="maximumMessageCount">Maximum number of dead-lettered messages to reset. Clamped between 1 and 100.</param>
    /// <returns>Number of inbox rows that were reset.</returns>
    public async Task<int> ResetInboxDeadLettersAsync(
        int maximumMessageCount = ReplayBatchLimit,
        CancellationToken cancellationToken = default)
    {
        var batchSize = Math.Clamp(maximumMessageCount, 1, ReplayBatchLimit);
        var inboxMessages = await db.InboxMessages
            .Where(row => row.Status == InboxMessageStatus.DeadLettered)
            .OrderBy(row => row.DeadLetteredAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var inboxMessage in inboxMessages)
        {
            ResetInboxForReplay(inboxMessage);
        }

        await db.SaveChangesAsync(cancellationToken);
        return inboxMessages.Count;
    }

    private void ResetOutboxForReplay(OutboxMessage outboxMessage)
    {
        outboxMessage.Attempts = 0;
        outboxMessage.LastError = null;
        outboxMessage.NextAttemptAt = clock.UtcNow;
        outboxMessage.DeadLetteredAt = null;
        outboxMessage.LockId = null;
        outboxMessage.LockedUntil = null;
    }

    private static void ResetInboxForReplay(InboxMessage inboxMessage)
    {
        inboxMessage.Status = InboxMessageStatus.Failed;
        inboxMessage.Attempts = 0;
        inboxMessage.LastError = null;
        inboxMessage.LastExceptionType = null;
        inboxMessage.LastFailedAt = null;
        inboxMessage.DeadLetteredAt = null;
    }
}
