using BuildingBlocks.Infrastructure;

namespace Sales.Infrastructure;

/// <summary>
/// Configuration for the recurring job that cancels pending orders left unpaid past their expiration.
/// </summary>
public sealed class CancelExpiredPendingOrdersJobOptions
{
    public RecurringJobSettings Schedule { get; init; } = new();

    /// <summary>How long a pending order may stay unpaid before it is cancelled.</summary>
    public int ExpirationMinutes { get; init; } = 30;

    /// <summary>Maximum number of expired orders cancelled per run.</summary>
    public int BatchSize { get; init; } = 100;

    /// <summary>
    /// Business parameters only constrain a job that actually runs.
    /// </summary>
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

        return ExpirationMinutes > 0
            && BatchSize > 0;
    }
}
