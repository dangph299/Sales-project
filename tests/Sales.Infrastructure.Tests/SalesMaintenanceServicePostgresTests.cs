using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Common.Interfaces;

namespace Sales.Infrastructure.Tests;

/// <summary>
/// Covers the batch recovery operations against PostgreSQL. They order by
/// <see cref="DateTimeOffset"/>, which the SQLite provider cannot translate, so these cannot run in
/// the in-memory suite.
/// </summary>
[Trait("Category", "Reliability")]
[Collection("SalesReliabilityPostgres")]
public sealed class SalesMaintenanceServicePostgresTests
{
    private static readonly DateTimeOffset CurrentUtc = DateTimeOffset.Parse("2026-07-15T10:00:00Z");

    private readonly PostgresReliabilityFixture _fixture;

    public SalesMaintenanceServicePostgresTests(PostgresReliabilityFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Replay_dead_letter_outbox_messages_respects_the_requested_batch_size()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await using var context = await CreateCleanContextAsync();
        context.OutboxMessages.AddRange(
            SalesMaintenanceSeedData.DeadLetteredOutboxMessage(CurrentUtc, CurrentUtc.AddMinutes(-3)),
            SalesMaintenanceSeedData.DeadLetteredOutboxMessage(CurrentUtc, CurrentUtc.AddMinutes(-2)),
            SalesMaintenanceSeedData.DeadLetteredOutboxMessage(CurrentUtc, CurrentUtc.AddMinutes(-1)));
        await context.SaveChangesAsync();

        var resetCount = await CreateService(context).ReplayDeadLetterOutboxMessagesAsync(2);

        Assert.Equal(2, resetCount);
        Assert.Equal(1, await context.OutboxMessages.CountAsync(row => row.DeadLetteredAt != null));
    }

    [SkippableFact]
    public async Task Replay_dead_letter_outbox_messages_ignores_already_published_messages()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await using var context = await CreateCleanContextAsync();
        var published = SalesMaintenanceSeedData.DeadLetteredOutboxMessage(CurrentUtc);
        published.ProcessedAt = CurrentUtc.AddMinutes(-10);
        context.OutboxMessages.Add(published);
        await context.SaveChangesAsync();

        var resetCount = await CreateService(context).ReplayDeadLetterOutboxMessagesAsync();

        Assert.Equal(0, resetCount);
        var reloaded = await context.OutboxMessages.SingleAsync(row => row.Id == published.Id);
        Assert.NotNull(reloaded.DeadLetteredAt);
    }

    [SkippableFact]
    public async Task Reset_inbox_dead_letters_respects_the_requested_batch_size()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await using var context = await CreateCleanContextAsync();
        context.InboxMessages.AddRange(
            SalesMaintenanceSeedData.InboxMessage(CurrentUtc, InboxMessageStatus.DeadLettered, CurrentUtc.AddMinutes(-3)),
            SalesMaintenanceSeedData.InboxMessage(CurrentUtc, InboxMessageStatus.DeadLettered, CurrentUtc.AddMinutes(-2)),
            SalesMaintenanceSeedData.InboxMessage(CurrentUtc, InboxMessageStatus.DeadLettered, CurrentUtc.AddMinutes(-1)));
        await context.SaveChangesAsync();

        var resetCount = await CreateService(context).ResetInboxDeadLettersAsync(2);

        Assert.Equal(2, resetCount);
        Assert.Equal(
            1,
            await context.InboxMessages.CountAsync(row => row.Status == InboxMessageStatus.DeadLettered));
    }

    [SkippableFact]
    public async Task Reset_inbox_dead_letters_ignores_messages_in_other_states()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await using var context = await CreateCleanContextAsync();
        context.InboxMessages.AddRange(
            SalesMaintenanceSeedData.InboxMessage(CurrentUtc, InboxMessageStatus.Processed),
            SalesMaintenanceSeedData.InboxMessage(CurrentUtc, InboxMessageStatus.Failed));
        await context.SaveChangesAsync();

        var resetCount = await CreateService(context).ResetInboxDeadLettersAsync();

        Assert.Equal(0, resetCount);
    }

    private static SalesMaintenanceService CreateService(SalesDbContext context)
    {
        return new SalesMaintenanceService(context, new FixedClock(CurrentUtc));
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

    private sealed class FixedClock(DateTimeOffset currentUtc) : IClock
    {
        public DateTimeOffset UtcNow { get; } = currentUtc;
    }

    private sealed class MaintenanceExecutionContext : IExecutionContext
    {
        public string Actor => "integration-test";

        public Guid CorrelationId => Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    }
}
