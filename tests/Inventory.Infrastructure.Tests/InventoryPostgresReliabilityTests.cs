using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Tests;

public sealed class InventoryPostgresReliabilityTests
{
    [Fact]
    public async Task Postgres_migration_contains_outbox_reliability_columns()
    {
        if (!ReliabilityTestSettings.Enabled) return;

        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(ReliabilityTestSettings.InventoryPostgresConnectionString)
            .Options;

        await using var db = new InventoryDbContext(options);
        await db.Database.MigrateAsync();
        await db.Outbox.ExecuteDeleteAsync();

        var row = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = "inventory.audit.v1",
            Payload = "{}",
            OccurredAt = DateTimeOffset.UtcNow,
            Attempts = OutboxMessage.MaxAttempts,
            DeadLetteredAt = DateTimeOffset.UtcNow,
            NextAttemptAt = null,
            LockId = Guid.NewGuid(),
            LockedUntil = DateTimeOffset.UtcNow.AddMinutes(5),
            LastError = "boom"
        };

        db.Outbox.Add(row);
        await db.SaveChangesAsync();

        var reloaded = await db.Outbox.SingleAsync(x => x.Id == row.Id);
        Assert.NotNull(reloaded.DeadLetteredAt);
        Assert.NotNull(reloaded.LockId);
        Assert.NotNull(reloaded.LockedUntil);
    }
}
