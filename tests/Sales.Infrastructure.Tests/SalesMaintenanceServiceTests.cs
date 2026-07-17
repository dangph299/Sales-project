using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Sales.Infrastructure.Tests;

/// <summary>
/// Covers the single-message recovery operations on SQLite so they run without Docker. The batch
/// operations order by <see cref="DateTimeOffset"/>, which SQLite cannot translate, so they are
/// covered against PostgreSQL in <see cref="SalesMaintenanceServicePostgresTests"/> instead.
/// </summary>
public sealed class SalesMaintenanceServiceTests
{
    private static readonly DateTimeOffset CurrentUtc = DateTimeOffset.Parse("2026-07-15T10:00:00Z");

    [Fact]
    public async Task Replay_outbox_message_resets_the_requested_message()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var deadLettered = SalesMaintenanceSeedData.DeadLetteredOutboxMessage(CurrentUtc);
        await SeedOutboxAsync(fixture, deadLettered);

        await using var context = fixture.CreateContext();
        var replayed = await CreateService(context).ReplayOutboxMessageAsync(deadLettered.Id);

        Assert.True(replayed);
        var reloaded = await context.OutboxMessages.SingleAsync(row => row.Id == deadLettered.Id);
        Assert.Equal(0, reloaded.Attempts);
        Assert.Null(reloaded.LastError);
        Assert.Null(reloaded.DeadLetteredAt);
        Assert.Null(reloaded.LockId);
        Assert.Null(reloaded.LockedUntil);
        Assert.Equal(CurrentUtc, reloaded.NextAttemptAt);
    }

    [Fact]
    public async Task Replay_outbox_message_reports_a_missing_message_without_changing_others()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var untouched = SalesMaintenanceSeedData.DeadLetteredOutboxMessage(CurrentUtc);
        await SeedOutboxAsync(fixture, untouched);

        await using var context = fixture.CreateContext();
        var replayed = await CreateService(context).ReplayOutboxMessageAsync(Guid.NewGuid());

        Assert.False(replayed);
        var reloaded = await context.OutboxMessages.SingleAsync(row => row.Id == untouched.Id);
        Assert.NotNull(reloaded.DeadLetteredAt);
        Assert.Equal(OutboxMessage.MaxAttempts, reloaded.Attempts);
    }

    [Fact]
    public async Task Reset_inbox_dead_letter_resets_the_requested_event()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var deadLettered = SalesMaintenanceSeedData.InboxMessage(CurrentUtc, InboxMessageStatus.DeadLettered);
        await SeedInboxAsync(fixture, deadLettered);

        await using var context = fixture.CreateContext();
        var wasReset = await CreateService(context).ResetInboxDeadLetterAsync(deadLettered.EventId);

        Assert.True(wasReset);
        var reloaded = await context.InboxMessages.SingleAsync(row => row.EventId == deadLettered.EventId);
        Assert.Equal(InboxMessageStatus.Failed, reloaded.Status);
        Assert.Equal(0, reloaded.Attempts);
        Assert.Null(reloaded.LastError);
        Assert.Null(reloaded.LastExceptionType);
        Assert.Null(reloaded.LastFailedAt);
        Assert.Null(reloaded.DeadLetteredAt);
    }

    [Fact]
    public async Task Reset_inbox_dead_letter_leaves_a_message_that_is_not_dead_lettered()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var processed = SalesMaintenanceSeedData.InboxMessage(CurrentUtc, InboxMessageStatus.Processed);
        await SeedInboxAsync(fixture, processed);

        await using var context = fixture.CreateContext();
        var wasReset = await CreateService(context).ResetInboxDeadLetterAsync(processed.EventId);

        Assert.False(wasReset);
        var reloaded = await context.InboxMessages.SingleAsync(row => row.EventId == processed.EventId);
        Assert.Equal(InboxMessageStatus.Processed, reloaded.Status);
        Assert.Equal(3, reloaded.Attempts);
    }

    [Fact]
    public async Task Replay_outbox_message_passes_the_cancellation_token_to_the_database()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var deadLettered = SalesMaintenanceSeedData.DeadLetteredOutboxMessage(CurrentUtc);
        await SeedOutboxAsync(fixture, deadLettered);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await using var context = fixture.CreateContext();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateService(context).ReplayOutboxMessageAsync(deadLettered.Id, cancellation.Token));
    }

    private static SalesMaintenanceService CreateService(SalesDbContext context)
    {
        return new SalesMaintenanceService(context, new FixedClock(CurrentUtc));
    }

    private static async Task SeedOutboxAsync(SqliteSalesFixture fixture, params OutboxMessage[] outboxMessages)
    {
        await using var context = fixture.CreateContext();
        context.OutboxMessages.AddRange(outboxMessages);
        await context.SaveChangesAsync();
    }

    private static async Task SeedInboxAsync(SqliteSalesFixture fixture, params InboxMessage[] inboxMessages)
    {
        await using var context = fixture.CreateContext();
        context.InboxMessages.AddRange(inboxMessages);
        await context.SaveChangesAsync();
    }

    private sealed class FixedClock(DateTimeOffset currentUtc) : IClock
    {
        public DateTimeOffset UtcNow { get; } = currentUtc;
    }
}
