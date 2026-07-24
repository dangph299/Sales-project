using BuildingBlocks.Infrastructure.Coordination.Redis;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using Xunit;

namespace BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// Exercises <see cref="RedisDistributedLeaseManager"/> against a real Redis, because acquire
/// contention, TTL expiry, and the compare-and-delete release script all depend on server-side
/// behavior that no in-memory double can faithfully reproduce.
/// </summary>
[Trait("Category", "Reliability")]
[Collection("RedisReliability")]
public sealed class RedisDistributedLeaseTests
{
    private readonly RedisReliabilityFixture _fixture;

    public RedisDistributedLeaseTests(RedisReliabilityFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Single_caller_acquires_the_lease()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        var manager = BuildManager(_fixture.Connection);
        var resource = UniqueResource();

        await using var lease = await manager.TryAcquireAsync(resource, LongLease());

        Assert.NotNull(lease);
        Assert.True(lease.IsHeld);
        Assert.Equal(resource, lease.Resource);
    }

    [SkippableFact]
    public async Task Two_callers_racing_the_same_resource_only_one_acquires()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        var manager = BuildManager(_fixture.Connection);
        var resource = UniqueResource();
        var options = new DistributedLeaseOptions { LeaseDuration = TimeSpan.FromSeconds(30) };

        var firstTask = manager.TryAcquireAsync(resource, options);
        var secondTask = manager.TryAcquireAsync(resource, options);
        var results = await Task.WhenAll(firstTask, secondTask);

        await using var first = results[0];
        await using var second = results[1];
        Assert.Single(results, lease => lease is not null);
    }

    [SkippableFact]
    public async Task Different_resources_acquire_independently()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        var manager = BuildManager(_fixture.Connection);

        await using var first = await manager.TryAcquireAsync(UniqueResource(), LongLease());
        await using var second = await manager.TryAcquireAsync(UniqueResource(), LongLease());

        Assert.NotNull(first);
        Assert.NotNull(second);
    }

    [SkippableFact]
    public async Task Dispose_releases_the_lease_so_the_resource_can_be_reacquired()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        var manager = BuildManager(_fixture.Connection);
        var resource = UniqueResource();
        var options = new DistributedLeaseOptions { LeaseDuration = TimeSpan.FromSeconds(30) };

        var first = await manager.TryAcquireAsync(resource, options);
        Assert.NotNull(first);
        await first.DisposeAsync();

        await using var second = await manager.TryAcquireAsync(resource, options);
        Assert.NotNull(second);
    }

    [SkippableFact]
    public async Task Dispose_is_idempotent()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        var manager = BuildManager(_fixture.Connection);
        var lease = await manager.TryAcquireAsync(UniqueResource(), LongLease());
        Assert.NotNull(lease);

        await lease.DisposeAsync();
        var exception = await Record.ExceptionAsync(() => lease.DisposeAsync().AsTask());

        Assert.Null(exception);
        Assert.False(lease.IsHeld);
    }

    [SkippableFact]
    public async Task Expired_owner_cannot_delete_a_later_owners_lease()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        var manager = BuildManager(_fixture.Connection);
        var resource = UniqueResource();
        var shortLease = new DistributedLeaseOptions { LeaseDuration = TimeSpan.FromMilliseconds(200) };

        var ownerA = await manager.TryAcquireAsync(resource, shortLease);
        Assert.NotNull(ownerA);

        await Task.Delay(TimeSpan.FromMilliseconds(400));

        var ownerB = await manager.TryAcquireAsync(resource, new DistributedLeaseOptions { LeaseDuration = TimeSpan.FromSeconds(30) });
        Assert.NotNull(ownerB);

        await ownerA.DisposeAsync();

        var remainingValue = await _fixture.Connection.GetDatabase().StringGetAsync("lock:" + resource);
        Assert.Equal(ownerB.OwnerToken, remainingValue);

        await ownerB.DisposeAsync();
    }

    [SkippableFact]
    public async Task Lease_expires_on_its_own_when_never_disposed()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        var manager = BuildManager(_fixture.Connection);
        var resource = UniqueResource();
        var shortLease = new DistributedLeaseOptions { LeaseDuration = TimeSpan.FromMilliseconds(200) };

        var lease = await manager.TryAcquireAsync(resource, shortLease);
        Assert.NotNull(lease);

        await Task.Delay(TimeSpan.FromMilliseconds(400));

        var remainingValue = await _fixture.Connection.GetDatabase().StringGetAsync("lock:" + resource);
        Assert.False(remainingValue.HasValue);
    }

    [SkippableFact]
    public async Task Compare_and_delete_script_only_removes_the_matching_owners_key()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        var database = _fixture.Connection.GetDatabase();
        var key = "lock:" + UniqueResource();
        await database.StringSetAsync(key, "owner-a");

        var deletedByWrongOwner = (long)(await database.ScriptEvaluateAsync(
            RedisLeaseScripts.CompareAndDelete, [(RedisKey)key], [(RedisValue)"owner-b"]))!;
        Assert.Equal(0, deletedByWrongOwner);
        Assert.Equal("owner-a", await database.StringGetAsync(key));

        var deletedByRightOwner = (long)(await database.ScriptEvaluateAsync(
            RedisLeaseScripts.CompareAndDelete, [(RedisKey)key], [(RedisValue)"owner-a"]))!;
        Assert.Equal(1, deletedByRightOwner);
        Assert.False((await database.StringGetAsync(key)).HasValue);
    }

    [Fact]
    public async Task Redis_unavailable_is_not_reported_as_lock_contention()
    {
        await using var unreachable = await ConnectionMultiplexer.ConnectAsync(
            "127.0.0.1:59999,abortConnect=false,connectTimeout=200,connectRetry=0,syncTimeout=200");
        var manager = BuildManager(unreachable);

        await Assert.ThrowsAnyAsync<RedisException>(
            () => manager.TryAcquireAsync(UniqueResource(), LongLease()));
    }

    private static RedisDistributedLeaseManager BuildManager(IConnectionMultiplexer connection)
    {
        return new RedisDistributedLeaseManager(connection, NullLogger<RedisDistributedLeaseManager>.Instance);
    }

    private static DistributedLeaseOptions LongLease()
    {
        return new DistributedLeaseOptions { LeaseDuration = TimeSpan.FromSeconds(30) };
    }

    private static string UniqueResource()
    {
        return $"tests:distributed-lease:{Guid.NewGuid():N}";
    }
}
