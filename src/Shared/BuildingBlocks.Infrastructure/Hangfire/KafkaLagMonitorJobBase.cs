using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Shared Hangfire job body for reading Kafka consumer lag through admin APIs without creating a
/// Kafka consumer.
/// </summary>
public abstract class KafkaLagMonitorJobBase<TDbContext>(
    TDbContext db,
    IConfiguration configuration,
    ILogger logger)
    where TDbContext : DbContext
{
    /// <summary>Updates service-specific Kafka lag gauges.</summary>
    protected abstract void SetKafkaConsumerLag(long lag, long partitions);

    /// <summary>Executes one Kafka consumer lag snapshot.</summary>
    protected async Task ExecuteCoreAsync(
        string groupId,
        IReadOnlyCollection<string> topics,
        int warningThreshold,
        int requestTimeoutSeconds,
        long lockKey,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await TryAcquireLockAsync(lockKey, cancellationToken))
        {
            logger.LogDebug("{DbContext} Kafka lag monitor skipped because another instance holds the lock", typeof(TDbContext).Name);
            return;
        }

        await ExecuteMonitorBatchAsync(groupId, topics, warningThreshold, requestTimeoutSeconds, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// Executes one Kafka consumer lag snapshot without opening a transaction or acquiring any
    /// lock. Read-only against Kafka admin APIs: duplicate execution only duplicates metrics.
    /// </summary>
    protected async Task ExecuteMonitorBatchAsync(
        string groupId,
        IReadOnlyCollection<string> topics,
        int warningThreshold,
        int requestTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(requestTimeoutSeconds);
        using var adminClient = CreateAdminClient();
        var topicPartitions = ReadTopicPartitions(adminClient, topics, timeout);
        if (topicPartitions.Count == 0)
        {
            SetKafkaConsumerLag(0, 0);
            logger.LogWarning("{DbContext} Kafka lag monitor found no partitions {GroupId}", typeof(TDbContext).Name, groupId);
            return;
        }

        var committedOffsets = await adminClient.ListConsumerGroupOffsetsAsync(
            [new ConsumerGroupTopicPartitions(groupId, topicPartitions)],
            new ListConsumerGroupOffsetsOptions { RequestTimeout = timeout });
        var committedByPartition = committedOffsets.Single().Partitions
            .Where(offset => !offset.Error.IsError)
            .ToDictionary(offset => offset.TopicPartition, offset => offset.Offset);

        var latestOffsets = await adminClient.ListOffsetsAsync(
            topicPartitions.Select(topicPartition => new TopicPartitionOffsetSpec
            {
                TopicPartition = topicPartition,
                OffsetSpec = OffsetSpec.Latest()
            }),
            new ListOffsetsOptions { RequestTimeout = timeout });

        var totalLag = 0L;
        var laggingPartitions = 0L;
        foreach (var latest in latestOffsets.ResultInfos.Select(result => result.TopicPartitionOffsetError))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (latest.Error.IsError)
            {
                logger.LogWarning(
                    "{DbContext} Kafka lag monitor could not read latest offset {Topic} {Partition} {ErrorCode}",
                    typeof(TDbContext).Name,
                    latest.Topic,
                    latest.Partition.Value,
                    latest.Error.Code);
                continue;
            }

            var committed = committedByPartition.TryGetValue(latest.TopicPartition, out var offset) && offset != Offset.Unset
                ? offset.Value
                : 0;
            var lag = Math.Max(0, latest.Offset.Value - committed);
            totalLag += lag;
            if (lag > 0)
            {
                laggingPartitions++;
            }
        }

        SetKafkaConsumerLag(totalLag, laggingPartitions);
        if (totalLag >= warningThreshold)
        {
            logger.LogWarning(
                "{DbContext} Kafka consumer lag threshold exceeded {GroupId} {Lag} {LaggingPartitions}",
                typeof(TDbContext).Name,
                groupId,
                totalLag,
                laggingPartitions);
        }
        else
        {
            logger.LogInformation(
                "{DbContext} Kafka consumer lag snapshot {GroupId} {Lag} {LaggingPartitions}",
                typeof(TDbContext).Name,
                groupId,
                totalLag,
                laggingPartitions);
        }
    }

    private IAdminClient CreateAdminClient()
    {
        var brokers = configuration.GetSection("Kafka:Brokers")
            .GetChildren()
            .Select(section => section.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        if (brokers.Length == 0)
        {
            brokers = ["kafka:9092"];
        }

        return new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = string.Join(",", brokers),
            ClientId = $"{typeof(TDbContext).Name.ToLowerInvariant()}-kafka-lag-monitor"
        }).Build();
    }

    private static List<TopicPartition> ReadTopicPartitions(
        IAdminClient adminClient,
        IEnumerable<string> topics,
        TimeSpan timeout)
    {
        var topicPartitions = new List<TopicPartition>();
        foreach (var topic in topics)
        {
            var metadata = adminClient.GetMetadata(topic, timeout);
            var topicMetadata = metadata.Topics.SingleOrDefault(current => current.Topic == topic);
            if (topicMetadata is null || topicMetadata.Error.IsError)
            {
                continue;
            }

            topicPartitions.AddRange(topicMetadata.Partitions.Select(partition =>
                new TopicPartition(topic, new Partition(partition.PartitionId))));
        }

        return topicPartitions;
    }

    private async Task<bool> TryAcquireLockAsync(long lockKey, CancellationToken cancellationToken)
    {
        return await db.Database
            .SqlQueryRaw<bool>("select pg_try_advisory_xact_lock({0}) as \"Value\"", lockKey)
            .SingleAsync(cancellationToken);
    }
}
