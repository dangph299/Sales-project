using BuildingBlocks.Infrastructure;

namespace Sales.Infrastructure.Tests;

/// <summary>
/// Shared inbox/outbox rows for the Sales maintenance recovery tests, used by both the SQLite and
/// PostgreSQL suites so they exercise identical starting state.
/// </summary>
internal static class SalesMaintenanceSeedData
{
    public static OutboxMessage DeadLetteredOutboxMessage(
        DateTimeOffset currentUtc,
        DateTimeOffset? deadLetteredAt = null)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = "sales.audit.v1",
            Payload = "{}",
            OccurredAt = currentUtc.AddMinutes(-5),
            Attempts = OutboxMessage.MaxAttempts,
            LastError = "boom",
            DeadLetteredAt = deadLetteredAt ?? currentUtc.AddMinutes(-1),
            LockId = Guid.NewGuid(),
            LockedUntil = currentUtc.AddMinutes(5)
        };
    }

    public static OutboxMessage PendingOutboxMessage(DateTimeOffset occurredAt)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = "sales.audit.v1",
            Payload = "{}",
            OccurredAt = occurredAt
        };
    }

    public static InboxMessage InboxMessage(
        DateTimeOffset currentUtc,
        InboxMessageStatus status,
        DateTimeOffset? deadLetteredAt = null)
    {
        return new InboxMessage
        {
            EventId = Guid.NewGuid(),
            Consumer = "sales-consumer",
            Status = status,
            Attempts = 3,
            LastError = "boom",
            LastExceptionType = "System.InvalidOperationException",
            LastFailedAt = currentUtc.AddMinutes(-2),
            DeadLetteredAt = status == InboxMessageStatus.DeadLettered
                ? deadLetteredAt ?? currentUtc.AddMinutes(-1)
                : null,
            ProcessedAt = currentUtc.AddMinutes(-1)
        };
    }
}
