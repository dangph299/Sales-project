using System.Linq.Expressions;
using Hangfire;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Shared helpers for registering Hangfire recurring jobs consistently.
/// </summary>
public static class RecurringJobManagerExtensions
{
    /// <summary>
    /// Adds or updates a recurring job with shared defaults.
    /// </summary>
    /// <typeparam name="TJob">Recurring job type resolved by Hangfire.</typeparam>
    /// <param name="recurringJobManager">Hangfire recurring job manager.</param>
    /// <param name="jobId">Stable recurring job identifier.</param>
    /// <param name="queue">Queue where the job should run.</param>
    /// <param name="cronExpression">Cron expression used by Hangfire.</param>
    /// <param name="jobExpression">Expression for the job method to execute.</param>
    public static void AddOrUpdateRecurringJob<TJob>(
        this IRecurringJobManager recurringJobManager,
        string jobId,
        string queue,
        string cronExpression,
        Expression<Func<TJob, Task>> jobExpression)
    {
        ArgumentNullException.ThrowIfNull(recurringJobManager);
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);
        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);
        ArgumentNullException.ThrowIfNull(jobExpression);

        recurringJobManager.AddOrUpdate(
            jobId,
            queue,
            jobExpression,
            cronExpression,
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
