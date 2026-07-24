using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sales.Application.Common.Interfaces;

namespace Sales.Infrastructure.Tests;

[Trait("Category", "Reliability")]
[Collection("SalesReliabilityPostgres")]
public sealed class MessagingReliabilityJobsPostgresTests
{
    private static readonly DateTimeOffset CurrentUtc = DateTimeOffset.Parse("2026-07-15T10:00:00Z");

    private readonly PostgresReliabilityFixture _fixture;

    public MessagingReliabilityJobsPostgresTests(PostgresReliabilityFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Replay_dead_letter_job_resets_inbox_dead_letters_for_redrive()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await using var context = await CreateCleanContextAsync();
        var deadLettered = SalesMaintenanceSeedData.InboxMessage(
            CurrentUtc,
            InboxMessageStatus.DeadLettered,
            CurrentUtc.AddMinutes(-10));
        deadLettered.Payload = "{}";
        context.InboxMessages.Add(deadLettered);
        await context.SaveChangesAsync();

        await new ReplayDeadLetterJob(
            context,
            new FixedClock(CurrentUtc),
            Options.Create(OptionsForReplayDeadLetter()),
            NullLogger<ReplayDeadLetterJob>.Instance).ExecuteAsync();

        var reloaded = await context.InboxMessages.SingleAsync(row => row.EventId == deadLettered.EventId);
        Assert.Equal(InboxMessageStatus.Failed, reloaded.Status);
        Assert.Equal(0, reloaded.Attempts);
        Assert.Null(reloaded.DeadLetteredAt);
        Assert.Equal(CurrentUtc.AddSeconds(5), reloaded.NextAttemptAt);
        Assert.Null(reloaded.LastError);
    }

    [SkippableFact]
    public async Task Inbox_cleanup_job_deletes_only_processed_rows_past_retention()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await using var context = await CreateCleanContextAsync();
        var oldProcessed = SalesMaintenanceSeedData.InboxMessage(
            CurrentUtc.AddDays(-30),
            InboxMessageStatus.Processed);
        var recentProcessed = SalesMaintenanceSeedData.InboxMessage(
            CurrentUtc.AddDays(-1),
            InboxMessageStatus.Processed);
        var failed = SalesMaintenanceSeedData.InboxMessage(
            CurrentUtc.AddDays(-30),
            InboxMessageStatus.Failed);
        context.InboxMessages.AddRange(oldProcessed, recentProcessed, failed);
        await context.SaveChangesAsync();

        await new InboxCleanupJob(
            context,
            new FixedClock(CurrentUtc),
            Options.Create(OptionsForInboxCleanup()),
            NullLogger<InboxCleanupJob>.Instance).ExecuteAsync();

        Assert.False(await context.InboxMessages.AnyAsync(row => row.EventId == oldProcessed.EventId));
        Assert.True(await context.InboxMessages.AnyAsync(row => row.EventId == recentProcessed.EventId));
        Assert.True(await context.InboxMessages.AnyAsync(row => row.EventId == failed.EventId));
    }

    [SkippableFact]
    public async Task Failed_outbox_retry_job_resets_terminal_failed_rows_and_signals_publisher()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await using var context = await CreateCleanContextAsync();
        var deadLettered = SalesMaintenanceSeedData.DeadLetteredOutboxMessage(CurrentUtc, CurrentUtc.AddMinutes(-10));
        var published = SalesMaintenanceSeedData.DeadLetteredOutboxMessage(CurrentUtc, CurrentUtc.AddMinutes(-9));
        published.ProcessedAt = CurrentUtc.AddMinutes(-1);
        context.OutboxMessages.AddRange(deadLettered, published);
        await context.SaveChangesAsync();
        var signal = new RecordingOutboxSignal();

        await new FailedOutboxRetryJob(
            context,
            new FixedClock(CurrentUtc),
            signal,
            Options.Create(OptionsForFailedOutboxRetry()),
            NullLogger<FailedOutboxRetryJob>.Instance).ExecuteAsync();

        var reloaded = await context.OutboxMessages.SingleAsync(row => row.Id == deadLettered.Id);
        Assert.Equal(0, reloaded.Attempts);
        Assert.Null(reloaded.DeadLetteredAt);
        Assert.Null(reloaded.LockId);
        Assert.Null(reloaded.LockedUntil);
        Assert.Equal(CurrentUtc.AddSeconds(7), reloaded.NextAttemptAt);
        Assert.Null(reloaded.LastError);
        Assert.True(signal.WasNotified);

        var untouched = await context.OutboxMessages.SingleAsync(row => row.Id == published.Id);
        Assert.NotNull(untouched.DeadLetteredAt);
    }

    [SkippableFact]
    public async Task Outbox_pending_monitor_reads_snapshot_without_changing_rows()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await using var context = await CreateCleanContextAsync();
        var pending = SalesMaintenanceSeedData.PendingOutboxMessage(CurrentUtc.AddMinutes(-20));
        var deadLettered = SalesMaintenanceSeedData.DeadLetteredOutboxMessage(CurrentUtc, CurrentUtc.AddMinutes(-5));
        context.OutboxMessages.AddRange(pending, deadLettered);
        await context.SaveChangesAsync();

        await new OutboxPendingMonitorJob(
            context,
            new FixedClock(CurrentUtc),
            Options.Create(OptionsForOutboxPendingMonitor()),
            NullLogger<OutboxPendingMonitorJob>.Instance).ExecuteAsync();

        var reloaded = await context.OutboxMessages.SingleAsync(row => row.Id == pending.Id);
        Assert.Null(reloaded.ProcessedAt);
        Assert.Null(reloaded.DeadLetteredAt);
        Assert.Equal(0, reloaded.Attempts);
    }

    private async Task<SalesDbContext> CreateCleanContextAsync()
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        var context = new SalesDbContext(options, new MaintenanceExecutionContext());
        await context.Database.MigrateAsync();
        await context.OutboxMessages.ExecuteDeleteAsync();
        await context.InboxMessages.ExecuteDeleteAsync();
        return context;
    }

    private static SalesRecurringJobsOptions OptionsForReplayDeadLetter()
    {
        return new SalesRecurringJobsOptions
        {
            ReplayDeadLetter = new ReplayDeadLetterJobOptions
            {
                BatchSize = 10,
                RetryDelaySeconds = 5
            }
        };
    }

    private static SalesRecurringJobsOptions OptionsForInboxCleanup()
    {
        return new SalesRecurringJobsOptions
        {
            InboxCleanup = new InboxCleanupJobOptions
            {
                BatchSize = 10,
                RetentionDays = 14
            }
        };
    }

    private static SalesRecurringJobsOptions OptionsForFailedOutboxRetry()
    {
        return new SalesRecurringJobsOptions
        {
            FailedOutboxRetry = new FailedOutboxRetryJobOptions
            {
                BatchSize = 10,
                RetryDelaySeconds = 7
            }
        };
    }

    private static SalesRecurringJobsOptions OptionsForOutboxPendingMonitor()
    {
        return new SalesRecurringJobsOptions
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

    private sealed class RecordingOutboxSignal : IOutboxSignal
    {
        public bool WasNotified { get; private set; }

        public void Notify() => WasNotified = true;

        public Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class MaintenanceExecutionContext : IExecutionContext
    {
        public string Actor => "integration-test";

        public Guid CorrelationId => Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    }
}
