using System.Collections.Concurrent;
using BuildingBlocks.Application;
using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Sales.Application;

namespace Sales.Infrastructure.Tests;

/// <summary>
/// Exercises the transactional-outbox retry and dead-letter guarantees against a real Postgres,
/// driving one <see cref="SalesOutboxPublisher"/> publish cycle at a time so the state machine is
/// deterministic (no background loop, no sleeps). Requires <c>RUN_RELIABILITY_TESTS=true</c>.
/// </summary>
[Trait("Category", "Reliability")]
public sealed class OutboxRetryReliabilityTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Transient_publish_failure_is_retried_and_published_exactly_once()
    {
        if (!ReliabilityTestSettings.Enabled) return;

        await EnsureMigratedAsync();

        var messageId = Guid.NewGuid();
        await SeedOutboxRowAsync(messageId, attempts: 0, nextAttemptAt: null);

        // Kafka is unavailable on the first attempt, then recovers.
        var transport = new ScriptedOutboxTransport(throwFirstAttempts: 1);
        var clock = new MutableClock(T0);
        var publisher = CreatePublisher(transport, clock);

        // Cycle 1: publish throws, the row survives with a scheduled retry (not dead-lettered).
        await RunCycleAsync(publisher);

        var afterFailure = await LoadRowAsync(messageId);
        Assert.Equal(1, afterFailure.Attempts);
        Assert.Null(afterFailure.ProcessedAt);
        Assert.Null(afterFailure.DeadLetteredAt);
        Assert.NotNull(afterFailure.NextAttemptAt);
        Assert.Equal(0, transport.SuccessCountFor(messageId));

        // Kafka recovers; advance past the exponential backoff window so the row is eligible again.
        clock.UtcNow = T0.AddSeconds(30);
        await RunCycleAsync(publisher);

        var afterRecovery = await LoadRowAsync(messageId);
        Assert.NotNull(afterRecovery.ProcessedAt);
        Assert.Null(afterRecovery.DeadLetteredAt);
        Assert.Null(afterRecovery.NextAttemptAt);
        // Published exactly once despite the earlier failure: no duplicate business message.
        Assert.Equal(1, transport.SuccessCountFor(messageId));
    }

    [Fact]
    public async Task Repeated_publish_failure_dead_letters_after_max_attempts()
    {
        if (!ReliabilityTestSettings.Enabled) return;

        await EnsureMigratedAsync();

        var messageId = Guid.NewGuid();
        // One attempt short of the cap and already eligible, so a single failing cycle crosses it.
        await SeedOutboxRowAsync(messageId, attempts: OutboxMessage.MaxAttempts - 1, nextAttemptAt: T0.AddMinutes(-1));

        var transport = new ScriptedOutboxTransport(throwFirstAttempts: int.MaxValue);
        var publisher = CreatePublisher(transport, new MutableClock(T0));

        await RunCycleAsync(publisher);

        var row = await LoadRowAsync(messageId);
        Assert.Equal(OutboxMessage.MaxAttempts, row.Attempts);
        Assert.NotNull(row.DeadLetteredAt);
        Assert.Null(row.NextAttemptAt);
        Assert.NotNull(row.LastError);
        Assert.Null(row.ProcessedAt);
        Assert.Equal(0, transport.SuccessCountFor(messageId));
    }

    private static SalesOutboxPublisher CreatePublisher(IOutboxPublisher transport, IClock clock) =>
        new(
            // The single-cycle seam touches neither the scope factory nor the wake-up signal.
            scopes: null!,
            publisher: transport,
            logger: NullLogger<SalesOutboxPublisher>.Instance,
            clock: clock,
            signal: null!,
            configuration: new ConfigurationBuilder().Build());

    private static async Task RunCycleAsync(SalesOutboxPublisher publisher)
    {
        // A fresh context per cycle mirrors the publisher's real per-cycle scope.
        await using var db = NewContext();
        await publisher.RunPublishCycleAsync(db);
    }

    private static async Task SeedOutboxRowAsync(Guid id, int attempts, DateTimeOffset? nextAttemptAt)
    {
        await using var db = NewContext();
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = id,
            Topic = KafkaTopics.SalesAudit,
            Payload = "{}",
            OccurredAt = T0.AddMinutes(-1),
            Attempts = attempts,
            NextAttemptAt = nextAttemptAt
        });
        await db.SaveChangesAsync();
    }

    private static async Task<OutboxMessage> LoadRowAsync(Guid id)
    {
        await using var db = NewContext();
        return await db.OutboxMessages.AsNoTracking().SingleAsync(x => x.Id == id);
    }

    private static async Task EnsureMigratedAsync()
    {
        await using var db = NewContext();
        await db.Database.MigrateAsync();
    }

    private static SalesDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseNpgsql(ReliabilityTestSettings.SalesPostgresConnectionString)
            .Options;
        return new SalesDbContext(options, new TestExecutionContext());
    }

    /// <summary>Fails the first N publish attempts per message id, then succeeds, tracking successes per id.</summary>
    private sealed class ScriptedOutboxTransport(int throwFirstAttempts) : IOutboxPublisher
    {
        private readonly ConcurrentDictionary<Guid, int> _attempts = new();
        private readonly ConcurrentDictionary<Guid, int> _successes = new();

        public Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            var attempt = _attempts.AddOrUpdate(message.Id, 1, (_, current) => current + 1);
            if (attempt <= throwFirstAttempts)
            {
                throw new InvalidOperationException("Kafka temporarily unavailable");
            }

            _successes.AddOrUpdate(message.Id, 1, (_, current) => current + 1);
            return Task.CompletedTask;
        }

        public int SuccessCountFor(Guid id) => _successes.TryGetValue(id, out var count) ? count : 0;
    }

    private sealed class MutableClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = now;
    }

    private sealed class TestExecutionContext : IExecutionContext
    {
        public string Actor => "integration-test";
        public Guid CorrelationId => Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    }
}
