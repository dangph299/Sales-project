using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;

namespace Sales.Infrastructure;

/// <summary>
/// Configuration for the recurring job that monitors Kafka consumer lag.
/// </summary>
public sealed class KafkaLagMonitorJobOptions
{
    public RecurringJobSettings Schedule { get; init; } = new();

    /// <summary>Consumer group whose committed offsets are monitored.</summary>
    public string GroupId { get; init; } = KafkaConsumerGroups.SalesInventoryResults;

    /// <summary>Topics consumed by the configured group.</summary>
    public string[] Topics { get; set; } =
    [
        KafkaTopics.StockReserved,
        KafkaTopics.StockRejected,
        KafkaTopics.StockReleased
    ];

    /// <summary>Total lag at or above which the job logs a warning.</summary>
    public int WarningThreshold { get; init; } = 100;

    /// <summary>Kafka admin request timeout in seconds.</summary>
    public int RequestTimeoutSeconds { get; init; } = 10;

    /// <summary>Business parameters only constrain a job that actually runs.</summary>
    public bool IsValid()
    {
        if (!Schedule.IsValid())
        {
            return false;
        }

        if (!Schedule.Enabled)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(GroupId)
            && Topics.Length > 0
            && Topics.All(topic => !string.IsNullOrWhiteSpace(topic))
            && WarningThreshold >= 0
            && RequestTimeoutSeconds > 0;
    }
}
