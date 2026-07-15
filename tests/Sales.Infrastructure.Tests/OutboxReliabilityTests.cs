using BuildingBlocks.Infrastructure;
using BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using Sales.Application;

namespace Sales.Infrastructure.Tests;

public sealed class OutboxReliabilityTests
{
    [Fact]
    public async Task Postgres_migration_contains_outbox_reliability_columns_and_replay_resets_dead_letter()
    {
        if (!ReliabilityTestSettings.Enabled) return;

        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseNpgsql(ReliabilityTestSettings.SalesPostgresConnectionString)
            .Options;

        await using var db = new SalesDbContext(options, new TestExecutionContext());
        await db.Database.MigrateAsync();
        await db.OutboxMessages.ExecuteDeleteAsync();

        var row = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = "sales.audit.v1",
            Payload = "{}",
            OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            Attempts = OutboxMessage.MaxAttempts,
            LastError = "boom",
            DeadLetteredAt = DateTimeOffset.UtcNow,
            LockedUntil = DateTimeOffset.UtcNow.AddMinutes(5),
            LockId = Guid.NewGuid()
        };
        db.OutboxMessages.Add(row);
        await db.SaveChangesAsync();

        var jobs = new MaintenanceJobs(db, null!, new SystemClock());
        Assert.True(await jobs.ReplayOutboxMessageAsync(row.Id));

        var reloaded = await db.OutboxMessages.SingleAsync(x => x.Id == row.Id);
        Assert.Null(reloaded.DeadLetteredAt);
        Assert.Null(reloaded.LastError);
        Assert.Null(reloaded.LockId);
        Assert.Null(reloaded.LockedUntil);
        Assert.Equal(0, reloaded.Attempts);
        Assert.NotNull(reloaded.NextAttemptAt);
    }

    private sealed class TestExecutionContext : IExecutionContext
    {
        public string Actor => "integration-test";
        public Guid CorrelationId => Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    }
}
