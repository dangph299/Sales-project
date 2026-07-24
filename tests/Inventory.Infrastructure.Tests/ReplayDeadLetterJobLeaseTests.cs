using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using BuildingBlocks.Infrastructure.Coordination.Redis;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Inventory.Infrastructure.Tests;

/// <summary>
/// Exercises <see cref="ReplayDeadLetterJob"/>'s Redis distributed-lease coordination in
/// isolation: skip when the lease is already held, and Redis connectivity failures propagating as
/// exceptions rather than being reported as ordinary lock contention.
/// </summary>
public sealed class ReplayDeadLetterJobLeaseTests
{
    private static readonly DateTimeOffset CurrentUtc = DateTimeOffset.Parse("2026-07-15T10:00:00Z");

    [Fact]
    public async Task Replay_dead_letter_job_skips_when_lease_is_already_held()
    {
        await using var context = await CreateContextAsync();
        var leaseManager = new NeverAcquiringLeaseManager();

        await new ReplayDeadLetterJob(
            context,
            new FixedClock(CurrentUtc),
            leaseManager,
            Options.Create(OptionsForReplayDeadLetter()),
            NullLogger<ReplayDeadLetterJob>.Instance).ExecuteAsync();

        Assert.Equal(0, await context.Inbox.CountAsync());
    }

    [Fact]
    public async Task Replay_dead_letter_job_propagates_redis_failures_instead_of_treating_them_as_lock_contention()
    {
        await using var context = await CreateContextAsync();
        var leaseManager = new FailingLeaseManager(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "boom"));

        await Assert.ThrowsAsync<RedisConnectionException>(() => new ReplayDeadLetterJob(
            context,
            new FixedClock(CurrentUtc),
            leaseManager,
            Options.Create(OptionsForReplayDeadLetter()),
            NullLogger<ReplayDeadLetterJob>.Instance).ExecuteAsync());
    }

    private static async Task<InventoryDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite(connection).Options;
        var context = new InventoryDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
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

    private sealed class FixedClock(DateTimeOffset currentUtc) : IClock
    {
        public DateTimeOffset UtcNow { get; } = currentUtc;
    }

    private sealed class NeverAcquiringLeaseManager : IDistributedLeaseManager
    {
        public Task<IDistributedLease?> TryAcquireAsync(
            string resource,
            DistributedLeaseOptions options,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IDistributedLease?>(null);
        }
    }

    private sealed class FailingLeaseManager(Exception exception) : IDistributedLeaseManager
    {
        public Task<IDistributedLease?> TryAcquireAsync(
            string resource,
            DistributedLeaseOptions options,
            CancellationToken cancellationToken = default)
        {
            throw exception;
        }
    }
}
