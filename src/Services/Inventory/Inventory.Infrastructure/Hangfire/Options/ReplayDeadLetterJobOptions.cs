using BuildingBlocks.Infrastructure;

namespace Inventory.Infrastructure;

/// <summary>
/// Configuration for the recurring job that resets inbound dead-letter messages for re-drive.
/// </summary>
public sealed class ReplayDeadLetterJobOptions
{
    public RecurringJobSettings Schedule { get; init; } = new();

    /// <summary>Maximum number of dead-lettered inbox rows reset per run.</summary>
    public int BatchSize { get; init; } = 100;

    /// <summary>Delay before the inbox re-drive service may process reset rows.</summary>
    public int RetryDelaySeconds { get; init; }

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
            && RetryDelaySeconds >= 0;
    }
}
