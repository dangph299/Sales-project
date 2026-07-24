using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using BuildingBlocks.Infrastructure.Coordination.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Inventory.Infrastructure.Tests;

[Trait("Category", "Reliability")]
[Collection("InventoryReliabilityPostgres")]
public sealed class InventoryMessagingReliabilityJobsPostgresTests
{
    private static readonly DateTimeOffset CurrentUtc = DateTimeOffset.Parse("2026-07-15T10:00:00Z");

    private readonly InventoryPostgresReliabilityFixture fixture;

    public InventoryMessagingReliabilityJobsPostgresTests(InventoryPostgresReliabilityFixture fixture)
    {
        this.fixture = fixture;
    }

    [SkippableFact]
    public async Task Replay_dead_letter_job_resets_inventory_inbox_dead_letters_for_redrive()
    {
        Skip.IfNot(fixture.IsAvailable, fixture.SkipReason);

        await using var context = await CreateCleanContextAsync();
        var deadLettered = InboxMessage(CurrentUtc, InboxMessageStatus.DeadLettered, CurrentUtc.AddMinutes(-10));
        deadLettered.Payload = "{}";
        context.Inbox.Add(deadLettered);
        await context.SaveChangesAsync();

        await new ReplayDeadLetterJob(
            context,
            new FixedClock(CurrentUtc),
            new AlwaysAcquiringLeaseManager(),
            Options.Create(OptionsForReplayDeadLetter()),
            NullLogger<ReplayDeadLetterJob>.Instance).ExecuteAsync();

        var reloaded = await context.Inbox.SingleAsync(row => row.EventId == deadLettered.EventId);
        Assert.Equal(InboxMessageStatus.Failed, reloaded.Status);
        Assert.Equal(0, reloaded.Attempts);
        Assert.Null(reloaded.DeadLetteredAt);
        Assert.Equal(CurrentUtc.AddSeconds(5), reloaded.NextAttemptAt);
        Assert.Null(reloaded.LastError);
    }

    [SkippableFact]
    public async Task Inbox_cleanup_job_deletes_only_inventory_processed_rows_past_retention()
    {
        Skip.IfNot(fixture.IsAvailable, fixture.SkipReason);

        await using var context = await CreateCleanContextAsync();
        var oldProcessed = InboxMessage(CurrentUtc.AddDays(-30), InboxMessageStatus.Processed);
        var recentProcessed = InboxMessage(CurrentUtc.AddDays(-1), InboxMessageStatus.Processed);
        var failed = InboxMessage(CurrentUtc.AddDays(-30), InboxMessageStatus.Failed);
        context.Inbox.AddRange(oldProcessed, recentProcessed, failed);
        await context.SaveChangesAsync();

        await new InboxCleanupJob(
            context,
            new FixedClock(CurrentUtc),
            Options.Create(OptionsForInboxCleanup()),
            NullLogger<InboxCleanupJob>.Instance).ExecuteAsync();

        Assert.False(await context.Inbox.AnyAsync(row => row.EventId == oldProcessed.EventId));
        Assert.True(await context.Inbox.AnyAsync(row => row.EventId == recentProcessed.EventId));
        Assert.True(await context.Inbox.AnyAsync(row => row.EventId == failed.EventId));
    }

    [SkippableFact]
    public async Task Failed_outbox_retry_job_resets_inventory_terminal_failed_rows_and_signals_publisher()
    {
        Skip.IfNot(fixture.IsAvailable, fixture.SkipReason);

        await using var context = await CreateCleanContextAsync();
        var deadLettered = DeadLetteredOutboxMessage(CurrentUtc, CurrentUtc.AddMinutes(-10));
        var published = DeadLetteredOutboxMessage(CurrentUtc, CurrentUtc.AddMinutes(-9));
        published.ProcessedAt = CurrentUtc.AddMinutes(-1);
        context.Outbox.AddRange(deadLettered, published);
        await context.SaveChangesAsync();
        var signal = new RecordingOutboxSignal();

        await new FailedOutboxRetryJob(
            context,
            new FixedClock(CurrentUtc),
            signal,
            Options.Create(OptionsForFailedOutboxRetry()),
            NullLogger<FailedOutboxRetryJob>.Instance).ExecuteAsync();

        var reloaded = await context.Outbox.SingleAsync(row => row.Id == deadLettered.Id);
        Assert.Equal(0, reloaded.Attempts);
        Assert.Null(reloaded.DeadLetteredAt);
        Assert.Null(reloaded.LockId);
        Assert.Null(reloaded.LockedUntil);
        Assert.Equal(CurrentUtc.AddSeconds(7), reloaded.NextAttemptAt);
        Assert.Null(reloaded.LastError);
        Assert.Equal(1, signal.NotificationCount);

        var untouched = await context.Outbox.SingleAsync(row => row.Id == published.Id);
        Assert.NotNull(untouched.DeadLetteredAt);
    }

    [SkippableFact]
    public async Task Failed_outbox_retry_job_does_not_claim_inventory_rows_with_active_lease()
    {
        Skip.IfNot(fixture.IsAvailable, fixture.SkipReason);

        await using var context = await CreateCleanContextAsync();
        var locked = DeadLetteredOutboxMessage(CurrentUtc, CurrentUtc.AddMinutes(-10));
        locked.LockId = Guid.NewGuid();
        locked.LockedUntil = CurrentUtc.AddMinutes(10);
        context.Outbox.Add(locked);
        await context.SaveChangesAsync();
        var signal = new RecordingOutboxSignal();

        await new FailedOutboxRetryJob(
            context,
            new FixedClock(CurrentUtc),
            signal,
            Options.Create(OptionsForFailedOutboxRetry()),
            NullLogger<FailedOutboxRetryJob>.Instance).ExecuteAsync();

        var reloaded = await context.Outbox.SingleAsync(row => row.Id == locked.Id);
        Assert.Equal(OutboxMessage.MaxAttempts, reloaded.Attempts);
        Assert.NotNull(reloaded.DeadLetteredAt);
        Assert.NotNull(reloaded.LockId);
        Assert.NotNull(reloaded.LockedUntil);
        Assert.Equal(0, signal.NotificationCount);
    }

    [SkippableFact]
    public async Task Concurrent_failed_outbox_retry_jobs_reset_inventory_row_once()
    {
        Skip.IfNot(fixture.IsAvailable, fixture.SkipReason);

        await using (var setup = await CreateCleanContextAsync())
        {
            setup.Outbox.Add(DeadLetteredOutboxMessage(CurrentUtc, CurrentUtc.AddMinutes(-10)));
            await setup.SaveChangesAsync();
        }

        await using var firstContext = CreateContext();
        await using var secondContext = CreateContext();
        var firstSignal = new RecordingOutboxSignal();
        var secondSignal = new RecordingOutboxSignal();
        var first = new FailedOutboxRetryJob(
            firstContext,
            new FixedClock(CurrentUtc),
            firstSignal,
            Options.Create(OptionsForFailedOutboxRetry()),
            NullLogger<FailedOutboxRetryJob>.Instance);
        var second = new FailedOutboxRetryJob(
            secondContext,
            new FixedClock(CurrentUtc),
            secondSignal,
            Options.Create(OptionsForFailedOutboxRetry()),
            NullLogger<FailedOutboxRetryJob>.Instance);

        await Task.WhenAll(first.ExecuteAsync(), second.ExecuteAsync());

        await using var verify = CreateContext();
        var reloaded = await verify.Outbox.SingleAsync();
        Assert.Null(reloaded.DeadLetteredAt);
        Assert.Null(reloaded.LockId);
        Assert.Null(reloaded.LockedUntil);
        Assert.Equal(1, firstSignal.NotificationCount + secondSignal.NotificationCount);
    }

    [SkippableFact]
    public async Task Outbox_pending_monitor_reads_inventory_snapshot_without_changing_rows()
    {
        Skip.IfNot(fixture.IsAvailable, fixture.SkipReason);

        await using var context = await CreateCleanContextAsync();
        var pending = PendingOutboxMessage(CurrentUtc.AddMinutes(-20));
        var deadLettered = DeadLetteredOutboxMessage(CurrentUtc, CurrentUtc.AddMinutes(-5));
        context.Outbox.AddRange(pending, deadLettered);
        await context.SaveChangesAsync();

        await new OutboxPendingMonitorJob(
            context,
            new FixedClock(CurrentUtc),
            Options.Create(OptionsForOutboxPendingMonitor()),
            NullLogger<OutboxPendingMonitorJob>.Instance).ExecuteAsync();

        var reloaded = await context.Outbox.SingleAsync(row => row.Id == pending.Id);
        Assert.Null(reloaded.ProcessedAt);
        Assert.Null(reloaded.DeadLetteredAt);
        Assert.Equal(0, reloaded.Attempts);
    }

    private InventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        return new InventoryDbContext(options);
    }

    private async Task<InventoryDbContext> CreateCleanContextAsync()
    {
        var context = CreateContext();
        await context.Database.MigrateAsync();
        await context.Outbox.ExecuteDeleteAsync();
        await context.Inbox.ExecuteDeleteAsync();
        return context;
    }

    private static InboxMessage InboxMessage(
        DateTimeOffset processedAt,
        InboxMessageStatus status,
        DateTimeOffset? deadLetteredAt = null)
    {
        return new InboxMessage
        {
            EventId = Guid.NewGuid(),
            ProcessedAt = processedAt,
            Status = status,
            Attempts = status == InboxMessageStatus.Processed ? 0 : 3,
            LastError = status == InboxMessageStatus.Processed ? null : "boom",
            LastExceptionType = status == InboxMessageStatus.Processed ? null : "InvalidOperationException",
            LastFailedAt = status == InboxMessageStatus.Processed ? null : processedAt,
            DeadLetteredAt = deadLetteredAt,
            OriginalConsumerGroup = "inventory-orders-v1",
            OriginalTopic = "sales.order-confirmation-requested.v1",
            OriginalPartition = 0,
            OriginalOffset = 10,
            Consumer = "inventory-orders-v1"
        };
    }

    private static OutboxMessage PendingOutboxMessage(DateTimeOffset occurredAt)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = "inventory.stock-reserved.v1",
            Payload = "{}",
            OccurredAt = occurredAt
        };
    }

    private static OutboxMessage DeadLetteredOutboxMessage(
        DateTimeOffset occurredAt,
        DateTimeOffset deadLetteredAt)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = "inventory.stock-reserved.v1",
            Payload = "{}",
            OccurredAt = occurredAt,
            Attempts = OutboxMessage.MaxAttempts,
            DeadLetteredAt = deadLetteredAt,
            LastError = "publish failed"
        };
    }

    private static InventoryRecurringJobsOptions OptionsForReplayDeadLetter()
    {
        return new InventoryRecurringJobsOptions
        {
            ReplayDeadLetter = new ReplayDeadLetterJobOptions
            {
                BatchSize = 10,
                RetryDelaySeconds = 5
            }
        };
    }

    private static InventoryRecurringJobsOptions OptionsForInboxCleanup()
    {
        return new InventoryRecurringJobsOptions
        {
            InboxCleanup = new InboxCleanupJobOptions
            {
                BatchSize = 10,
                RetentionDays = 14
            }
        };
    }

    private static InventoryRecurringJobsOptions OptionsForFailedOutboxRetry()
    {
        return new InventoryRecurringJobsOptions
        {
            FailedOutboxRetry = new FailedOutboxRetryJobOptions
            {
                BatchSize = 10,
                RetryDelaySeconds = 7
            }
        };
    }

    private static InventoryRecurringJobsOptions OptionsForOutboxPendingMonitor()
    {
        return new InventoryRecurringJobsOptions
        {
            OutboxPendingMonitor = new OutboxPendingMonitorJobOptions
            {
                BacklogWarningThreshold = 1,
                OldestPendingWarningSeconds = 1
            }
        };
    }

    private sealed class FixedClock(DateTimeOffset currentUtc) : IClock
    {
        public DateTimeOffset UtcNow { get; } = currentUtc;
    }

    private sealed class AlwaysAcquiringLeaseManager : IDistributedLeaseManager
    {
        public Task<IDistributedLease?> TryAcquireAsync(
            string resource,
            DistributedLeaseOptions options,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IDistributedLease?>(new AcquiredLease(resource));
        }

        private sealed class AcquiredLease(string resource) : IDistributedLease
        {
            public string Resource { get; } = resource;

            public string OwnerToken { get; } = Guid.NewGuid().ToString("N");

            public bool IsHeld => true;

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingOutboxSignal : IOutboxSignal
    {
        public int NotificationCount { get; private set; }

        public void Notify() => NotificationCount++;

        public Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
