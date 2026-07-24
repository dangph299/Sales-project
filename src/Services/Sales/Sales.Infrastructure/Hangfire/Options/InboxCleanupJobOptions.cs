using BuildingBlocks.Infrastructure;

namespace Sales.Infrastructure;

/// <summary>
/// Configuration for the recurring job that deletes processed inbox rows past retention.
/// </summary>
public sealed class InboxCleanupJobOptions
{
    public RecurringJobSettings Schedule { get; init; } = new();

    /// <summary>Maximum number of processed inbox rows deleted per run.</summary>
    public int BatchSize { get; init; } = 500;

    /// <summary>Number of days to retain processed inbox rows.</summary>
    public int RetentionDays { get; init; } = 14;

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

        return BatchSize > 0
            && RetentionDays > 0;
    }
}
