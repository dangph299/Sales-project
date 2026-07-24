using BuildingBlocks.Infrastructure;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inventory.Infrastructure;

/// <summary>
/// Hangfire adapter that reads Inventory Kafka consumer lag without creating a consumer.
/// </summary>
public sealed class KafkaLagMonitorJob(
    InventoryDbContext db,
    IConfiguration configuration,
    IOptions<InventoryRecurringJobsOptions> options,
    ILogger<KafkaLagMonitorJob> logger) : KafkaLagMonitorJobBase<InventoryDbContext>(db, configuration, logger)
{
    /// <summary>
    /// Executes one Inventory Kafka lag snapshot.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var jobOptions = options.Value.KafkaLagMonitor;
        await ExecuteCoreAsync(
            jobOptions.GroupId,
            jobOptions.Topics,
            jobOptions.WarningThreshold,
            jobOptions.RequestTimeoutSeconds,
            InventoryMessagingJobLockKeys.KafkaLagMonitor,
            cancellationToken);
    }

    /// <inheritdoc/>
    protected override void SetKafkaConsumerLag(long lag, long partitions) => InventoryMetrics.SetKafkaConsumerLag(lag, partitions);
}
