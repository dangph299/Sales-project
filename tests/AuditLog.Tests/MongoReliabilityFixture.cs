using Testcontainers.MongoDb;
using Xunit;

namespace AuditLog.Tests;

/// <summary>
/// Starts a real MongoDB container for the audit reliability suite so the unique-event-id dedup index
/// is exercised against the real engine. When Docker is unavailable the fixture reports
/// <see cref="IsAvailable"/> as <see langword="false"/> so tests skip visibly instead of passing
/// silently.
/// </summary>
public sealed class MongoReliabilityFixture : IAsyncLifetime
{
    private MongoDbContainer? _container;

    /// <summary>Gets whether the MongoDB container started and is usable.</summary>
    public bool IsAvailable { get; private set; }

    /// <summary>Gets the reason the reliability tests were skipped when the container is unavailable.</summary>
    public string SkipReason { get; private set; } = "Docker is not available to start the MongoDB test container.";

    /// <summary>Gets the connection string for the running container, or an empty string when unavailable.</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        try
        {
            _container = new MongoDbBuilder().WithImage("mongo:8").Build();
            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
            IsAvailable = true;
        }
        catch (Exception exception)
        {
            IsAvailable = false;
            SkipReason = $"MongoDB test container could not start: {exception.Message}";
        }
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_container is not null && IsAvailable)
        {
            await _container.DisposeAsync();
        }
    }
}
