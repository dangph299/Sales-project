using BuildingBlocks.Infrastructure;

namespace Sales.Infrastructure;

public sealed class CancelExpiredPendingOrdersJobOptions : RecurringJobScheduleOptions
{
    public int ExpirationMinutes { get; init; } = 30;

    public int BatchSize { get; init; } = 100;

    public override bool IsValid()
    {
        if (!base.IsValid())
        {
            return false;
        }

        if (!Enabled)
        {
            return true;
        }

        return ExpirationMinutes > 0
            && BatchSize > 0;
    }
}
