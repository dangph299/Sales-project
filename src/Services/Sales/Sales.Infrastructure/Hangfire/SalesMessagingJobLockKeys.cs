namespace Sales.Infrastructure;

/// <summary>
/// Stable PostgreSQL advisory lock keys for Sales messaging recurring jobs.
/// </summary>
public static class SalesMessagingJobLockKeys
{
    /// <summary>Lock key for Sales inbound dead-letter replay.</summary>
    public const long ReplayDeadLetter = 7_281_002_001;

    /// <summary>Lock key for Sales processed inbox cleanup.</summary>
    public const long InboxCleanup = 7_281_002_002;

    /// <summary>Lock key for Sales failed outbox retry reset.</summary>
    public const long FailedOutboxRetry = 7_281_002_003;

    /// <summary>Lock key for Sales outbox pending monitor.</summary>
    public const long OutboxPendingMonitor = 7_281_002_004;

    /// <summary>Lock key for Sales Kafka lag monitor.</summary>
    public const long KafkaLagMonitor = 7_281_002_005;
}
