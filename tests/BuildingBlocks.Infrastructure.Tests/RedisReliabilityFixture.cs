using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// Starts a real Redis container for the distributed lease reliability suite so acquire/release
/// semantics are exercised against the real engine, including its Lua scripting. When Docker is
/// unavailable the fixture reports <see cref="IsAvailable"/> as <see langword="false"/> so tests
/// skip visibly instead of passing silently.
/// </summary>
public sealed class RedisReliabilityFixture : IAsyncLifetime
{
    private RedisContainer? _container;

    /// <summary>Gets whether the Redis container started and is usable.</summary>
    public bool IsAvailable { get; private set; }

    /// <summary>Gets the reason the reliability tests were skipped when the container is unavailable.</summary>
    public string SkipReason { get; private set; } = "Docker is not available to start the Redis test container.";

    /// <summary>Gets a connection multiplexer to the running container. Only valid when <see cref="IsAvailable"/>.</summary>
    public IConnectionMultiplexer Connection { get; private set; } = null!;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        try
        {
            _container = new RedisBuilder("redis:8-alpine").Build();
            await _container.StartAsync();
            Connection = await ConnectionMultiplexer.ConnectAsync(_container.GetConnectionString());
            IsAvailable = true;
        }
        catch (Exception exception)
        {
            IsAvailable = false;
            SkipReason = $"Redis test container could not start: {exception.Message}";
        }
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (!IsAvailable)
        {
            return;
        }

        await Connection.DisposeAsync();
        await _container!.DisposeAsync();
    }
}
