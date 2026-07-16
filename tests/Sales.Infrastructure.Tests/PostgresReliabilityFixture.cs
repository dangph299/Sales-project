using Testcontainers.PostgreSql;
using Xunit;

namespace Sales.Infrastructure.Tests;

/// <summary>
/// Starts a real PostgreSQL container for the reliability suite so EF Core behavior (concurrency
/// tokens, outbox/inbox queries, <see cref="DateTimeOffset"/> comparisons) is exercised against the
/// same engine used in production. When Docker is unavailable the fixture reports
/// <see cref="IsAvailable"/> as <see langword="false"/> so tests skip visibly instead of passing
/// silently.
/// </summary>
public sealed class PostgresReliabilityFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    /// <summary>Gets whether the PostgreSQL container started and is usable.</summary>
    public bool IsAvailable { get; private set; }

    /// <summary>Gets the reason the reliability tests were skipped when the container is unavailable.</summary>
    public string SkipReason { get; private set; } = "Docker is not available to start the PostgreSQL test container.";

    /// <summary>Gets the connection string for the running container, or an empty string when unavailable.</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        try
        {
            // Build is inside the try because it eagerly probes Docker and throws when it is absent.
            _container = new PostgreSqlBuilder().WithImage("postgres:17-alpine").Build();
            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
            IsAvailable = true;
        }
        catch (Exception exception)
        {
            IsAvailable = false;
            SkipReason = $"PostgreSQL test container could not start: {exception.Message}";
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
