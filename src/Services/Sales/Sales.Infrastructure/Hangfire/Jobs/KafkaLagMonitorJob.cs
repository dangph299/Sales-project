using BuildingBlocks.Infrastructure;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sales.Infrastructure;

/// <summary>
/// Hangfire adapter that reads Kafka consumer lag through admin APIs without creating a consumer.
/// </summary>
public sealed class KafkaLagMonitorJob(
    SalesDbContext db,
    IConfiguration configuration,
    IOptions<SalesRecurringJobsOptions> options,
    ILogger<KafkaLagMonitorJob> logger) : KafkaLagMonitorJobBase<SalesDbContext>(db, configuration, logger)
{
    /// <summary>
    /// Executes one Kafka lag snapshot.
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
            SalesMessagingJobLockKeys.KafkaLagMonitor,
            cancellationToken);
    }

    /// <inheritdoc/>
    protected override void SetKafkaConsumerLag(long lag, long partitions) => SalesMetrics.SetKafkaConsumerLag(lag, partitions);
}
