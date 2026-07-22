using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Sales.Infrastructure.Tests;

/// <summary>
/// Provides a real PostgreSQL for the reliability suite. When none can be reached the fixture
/// reports <see cref="IsAvailable"/> as <see langword="false"/> so tests skip visibly instead of
/// passing silently.
/// </summary>
/// <remarks>
/// A connection string in <c>SALES_TEST_POSTGRES</c> is used in preference to starting a container.
/// The database it names is dropped and recreated by the migration tests, so it must be a dedicated
/// test database and never a development one.
/// </remarks>
public sealed class PostgresReliabilityFixture : IAsyncLifetime
{
    private const string ConnectionStringVariable = "SALES_TEST_POSTGRES";

    private PostgreSqlContainer? _container;

    /// <summary>Gets whether a PostgreSQL instance is reachable and usable.</summary>
    public bool IsAvailable { get; private set; }

    /// <summary>Gets the reason the reliability tests were skipped when none is available.</summary>
    public string SkipReason { get; private set; } = "Docker is not available to start the PostgreSQL test container.";

    /// <summary>Gets the connection string for the running instance, or an empty string when unavailable.</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var configuredConnectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            await UseConfiguredServerAsync(configuredConnectionString);
            return;
        }

        await StartContainerAsync();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    private async Task UseConfiguredServerAsync(string configuredConnectionString)
    {
        try
        {
            // Probed against the maintenance database, because the test database itself is dropped
            // and recreated by the migration tests and may legitimately not exist yet.
            var serverConnectionString = new NpgsqlConnectionStringBuilder(configuredConnectionString)
            {
                Database = "postgres"
            }.ConnectionString;

            await using var connection = new NpgsqlConnection(serverConnectionString);
            await connection.OpenAsync();

            ConnectionString = configuredConnectionString;
            IsAvailable = true;
        }
        catch (Exception exception)
        {
            IsAvailable = false;
            SkipReason = $"{ConnectionStringVariable} is set but the server could not be reached: {exception.Message}";
        }
    }

    private async Task StartContainerAsync()
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
}
