using BuildingBlocks.Infrastructure;

namespace Sales.Infrastructure;

/// <summary>
/// Configuration for the recurring job that monitors pending outbox health.
/// </summary>
public sealed class OutboxPendingMonitorJobOptions
{
    public RecurringJobSettings Schedule { get; init; } = new();

    /// <summary>Backlog count at or above which the job logs a warning.</summary>
    public int BacklogWarningThreshold { get; init; } = 100;

    /// <summary>Oldest pending message age at or above which the job logs a warning.</summary>
    public int OldestPendingWarningSeconds { get; init; } = 300;

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

        return BacklogWarningThreshold >= 0
            && OldestPendingWarningSeconds >= 0;
    }
}
