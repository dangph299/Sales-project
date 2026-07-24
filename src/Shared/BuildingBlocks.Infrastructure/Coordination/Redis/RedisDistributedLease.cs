using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BuildingBlocks.Infrastructure.Coordination.Redis;

/// <summary>
/// A lease acquired in Redis via <c>SET key token NX PX ttl</c>. Released with a Lua
/// compare-and-delete so an owner whose TTL already expired can never delete a later owner's key.
/// </summary>
internal sealed class RedisDistributedLease(
    IDatabase database,
    string key,
    string resource,
    string ownerToken,
    ILogger logger) : IDistributedLease
{
    private int _released;

    public string Resource { get; } = resource;

    public string OwnerToken { get; } = ownerToken;

    public bool IsHeld => _released == 0;

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _released, 1) != 0)
        {
            return;
        }

        try
        {
            await database.ScriptEvaluateAsync(RedisLeaseScripts.CompareAndDelete, [key], [OwnerToken]);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(
                exception,
                "Failed to release distributed lease for resource {Resource}; it will expire on its own via TTL",
                Resource);
        }
    }
}
