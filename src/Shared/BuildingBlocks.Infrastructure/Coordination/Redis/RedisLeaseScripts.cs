namespace BuildingBlocks.Infrastructure.Coordination.Redis;

/// <summary>
/// Lua scripts shared by the Redis lease implementation.
/// </summary>
internal static class RedisLeaseScripts
{
    /// <summary>
    /// Deletes the key only if it still holds the caller's own owner token, so a lease whose TTL
    /// already expired can never delete a different owner's subsequent acquisition of the same key.
    /// </summary>
    public const string CompareAndDelete =
        """
        if redis.call("get", KEYS[1]) == ARGV[1] then
            return redis.call("del", KEYS[1])
        end

        return 0
        """;
}
