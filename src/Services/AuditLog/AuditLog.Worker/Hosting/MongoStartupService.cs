using AuditLog.Infrastructure;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AuditLog.Worker;

/// <summary>
/// Hosted service that waits for MongoDB to become reachable (retrying with a fixed delay) and
/// ensures the audit store's indexes exist, exactly once, before the worker starts consuming.
/// </summary>
public sealed class MongoStartupService : IHostedService
{
    private readonly IMongoDatabase _database;
    private readonly IAuditWriter _writer;
    private readonly ILogger<MongoStartupService> _logger;

    /// <summary>
    /// Initializes the service with the MongoDB database and audit writer to prepare on startup.
    /// </summary>
    /// <param name="database">Audit MongoDB database.</param>
    /// <param name="writer">Audit writer.</param>
    /// <param name="logger">Logger.</param>
    public MongoStartupService(IMongoDatabase database, IAuditWriter writer, ILogger<MongoStartupService> logger)
    {
        _database = database;
        _writer = writer;
        _logger = logger;
    }

    /// <summary>
    /// Pings MongoDB (retrying up to 20 times, 2 seconds apart) and ensures indexes exist once it
    /// responds.
    /// </summary>
    /// <param name="cancellationToken">Host shutdown token.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 20; attempt++)
        {
            try
            {
                await _database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
                await _writer.EnsureIndexesAsync(cancellationToken);
                _logger.LogInformation("Mongo audit store is ready. Database={Database}, Collection=events", _database.DatabaseNamespace.DatabaseName);
                return;
            }
            catch (Exception ex) when (attempt < 20 && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Mongo audit store is not ready. Attempt {Attempt}/20", attempt);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        await _database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
