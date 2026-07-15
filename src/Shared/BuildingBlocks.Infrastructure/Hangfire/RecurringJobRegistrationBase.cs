using Hangfire;

namespace BuildingBlocks.Infrastructure;

public abstract class RecurringJobRegistrationBase<TOptions> : IRecurringJobRegistration
    where TOptions : RecurringJobScheduleOptions
{
    protected RecurringJobRegistrationBase(IRecurringJobManager recurringJobManager, TOptions options)
    {
        RecurringJobManager = recurringJobManager;
        Options = options;
    }

    protected IRecurringJobManager RecurringJobManager { get; }

    protected TOptions Options { get; }

    protected abstract string JobId { get; }

    public void Register()
    {
        if (!Options.Enabled)
        {
            RemoveIfExists();
            return;
        }

        AddOrUpdate();
    }

    protected abstract void AddOrUpdate();

    protected virtual void RemoveIfExists()
    {
        RecurringJobManager.RemoveIfExists(JobId);
    }

    protected static RecurringJobOptions CreateUtcRecurringJobOptions()
    {
        return new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Utc
        };
    }
}
