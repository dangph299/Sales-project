using System.Linq.Expressions;
using Hangfire;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Framework-level Hangfire registration shared by every service. Knows nothing about which jobs
/// exist: each service names its own job identifiers, settings, and job classes.
/// </summary>
public static class RecurringJobManagerExtensions
{
    /// <summary>
    /// Schedules a recurring job from its settings. An enabled job is added or updated on the queue
    /// and cron it names; a disabled job is removed from Hangfire storage so it stops firing rather
    /// than being left behind on its previous schedule.
    /// </summary>
    /// <param name="recurringJobId">Stable recurring job identifier owned by the calling service.</param>
    /// <param name="settings">Schedule settings for this job.</param>
    /// <param name="jobExpression">Job method Hangfire invokes on each run.</param>
    /// <exception cref="ArgumentException">Thrown when an enabled job omits its queue or cron expression.</exception>
    public static void ScheduleRecurringJob<TJob>(
        this IRecurringJobManager recurringJobManager,
        string recurringJobId,
        RecurringJobSettings settings,
        Expression<Func<TJob, Task>> jobExpression)
    {
        ArgumentNullException.ThrowIfNull(recurringJobManager);
        ArgumentException.ThrowIfNullOrWhiteSpace(recurringJobId);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(jobExpression);

        if (!settings.Enabled)
        {
            recurringJobManager.RemoveIfExists(recurringJobId);
            return;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Queue);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Cron);

        recurringJobManager.AddOrUpdate(
            recurringJobId,
            settings.Queue,
            jobExpression,
            settings.Cron,
            CreateDefaultRecurringJobOptions());
    }

    private static RecurringJobOptions CreateDefaultRecurringJobOptions()
    {
        return new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Utc
        };
    }
}
