using Hangfire;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Applies the enable/disable flow shared by every recurring job definition: an enabled job is added
/// or updated, a disabled job is removed from Hangfire storage so it stops firing.
/// </summary>
public abstract class RecurringJobDefinitionBase : IRecurringJobDefinition
{
    protected RecurringJobDefinitionBase(IRecurringJobManager recurringJobManager, RecurringJobSettings settings)
    {
        RecurringJobManager = recurringJobManager;
        Settings = settings;
    }

    protected IRecurringJobManager RecurringJobManager { get; }

    protected RecurringJobSettings Settings { get; }

    protected abstract string JobId { get; }

    public void Register()
    {
        if (!Settings.Enabled)
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
}
