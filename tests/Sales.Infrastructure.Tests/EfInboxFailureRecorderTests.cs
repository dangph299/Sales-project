using System.Text.Json;
using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Sales.Infrastructure.Tests;

public sealed class EfInboxFailureRecorderTests
{
    [Fact]
    public async Task RecordFailure_tracks_attempts_and_dead_letters_after_threshold()
    {
        await using var db = new TestInboxDbContext();
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var recorder = new TestInboxFailureRecorder(
            db,
            Options.Create(new InboxConsumerOptions { MaxAttempts = 2 }));
        var envelope = CreateEnvelope();
        var context = new InboundMessageContext("orders.v1", "inventory-v1", 3, 42);

        var first = await recorder.RecordFailureAsync(
            envelope,
            context,
            new InvalidOperationException("missing inventory"));
        var second = await recorder.RecordFailureAsync(
            envelope,
            context,
            new InvalidOperationException("missing inventory"));

        var row = await db.Inbox.SingleAsync(x => x.EventId == envelope.EventId);
        Assert.Equal(new InboundFailureResult(1, false), first);
        Assert.Equal(new InboundFailureResult(2, true), second);
        Assert.Equal(InboxMessageStatus.DeadLettered, row.Status);
        Assert.Equal(2, row.Attempts);
        Assert.NotNull(row.DeadLetteredAt);
        Assert.Equal("orders.v1", row.OriginalTopic);
        Assert.Equal("inventory-v1", row.OriginalConsumerGroup);
        Assert.Equal(3, row.OriginalPartition);
        Assert.Equal(42, row.OriginalOffset);
        Assert.Equal(typeof(InvalidOperationException).FullName, row.LastExceptionType);
        Assert.Equal("missing inventory", row.LastError);
        // Once dead-lettered there is no next attempt scheduled.
        Assert.Null(row.NextAttemptAt);
        // The envelope is retained so the re-drive service can replay it.
        Assert.NotNull(row.Payload);
    }

    [Fact]
    public async Task RecordFailure_below_threshold_retains_payload_and_schedules_next_attempt()
    {
        await using var db = new TestInboxDbContext();
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var recorder = new TestInboxFailureRecorder(
            db,
            Options.Create(new InboxConsumerOptions { MaxAttempts = 5 }));
        var envelope = CreateEnvelope();
        var context = new InboundMessageContext("orders.v1", "inventory-v1", 3, 42);

        var before = DateTimeOffset.UtcNow;
        var result = await recorder.RecordFailureAsync(
            envelope,
            context,
            new InvalidOperationException("transient"));

        var row = await db.Inbox.SingleAsync(x => x.EventId == envelope.EventId);
        Assert.Equal(new InboundFailureResult(1, false), result);
        Assert.Equal(InboxMessageStatus.Failed, row.Status);
        Assert.Null(row.DeadLetteredAt);
        Assert.NotNull(row.NextAttemptAt);
        Assert.True(row.NextAttemptAt > before, "next attempt should be scheduled in the future");
        var storedEnvelope = JsonSerializer.Deserialize<EventEnvelope>(row.Payload!);
        Assert.Equal(envelope.EventId, storedEnvelope!.EventId);
    }

    private static EventEnvelope CreateEnvelope()
    {
        using var doc = JsonDocument.Parse("""{"value":1}""");
        return new EventEnvelope(
            Guid.NewGuid(),
            "TestEvent",
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            null,
            DateTimeOffset.UtcNow,
            "test",
            doc.RootElement.Clone());
    }

    private sealed class TestInboxDbContext() : DbContext(
        new DbContextOptionsBuilder<TestInboxDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options)
    {
        public DbSet<InboxMessage> Inbox => Set<InboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InboxMessage>(entity =>
            {
                entity.HasKey(x => x.EventId);
                entity.Property(x => x.Consumer).HasMaxLength(64);
                entity.Property(x => x.LastExceptionType).HasMaxLength(512);
                entity.Property(x => x.LastError).HasMaxLength(2000);
                entity.Property(x => x.OriginalTopic).HasMaxLength(256);
                entity.Property(x => x.OriginalConsumerGroup).HasMaxLength(256);
            });
        }
    }

    private sealed class TestInboxFailureRecorder : EfInboxFailureRecorder<TestInboxDbContext>
    {
        private readonly TestInboxDbContext _db;

        public TestInboxFailureRecorder(TestInboxDbContext db, IOptions<InboxConsumerOptions> options)
            : base(db, options)
        {
            _db = db;
        }

        protected override DbSet<InboxMessage> Inbox => _db.Inbox;

        protected override string Consumer => "test-consumer";
    }
}
