using BuildingBlocks.Infrastructure;
using Hangfire;
using Hangfire.Common;

namespace BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// Exercises the shared Hangfire registration helper through a job type that belongs to no service,
/// proving any service can reuse it without depending on Sales.
/// </summary>
public sealed class RecurringJobManagerExtensionsTests
{
    private const string RecurringJobId = "reporting:rebuild-daily-totals";

    [Fact]
    public void Enabled_job_is_scheduled_on_its_configured_queue_and_cron()
    {
        var recurringJobManager = new RecordingRecurringJobManager();

        recurringJobManager.ScheduleRecurringJob<RebuildDailyTotalsJob>(
            RecurringJobId,
            new RecurringJobSettings { Enabled = true, Queue = "reporting", Cron = "0 3 * * *" },
            rebuildDailyTotalsJob => rebuildDailyTotalsJob.RebuildAsync());

        var scheduled = Assert.Single(recurringJobManager.ScheduledJobs);
        Assert.Equal(RecurringJobId, scheduled.RecurringJobId);
        Assert.Equal("reporting", scheduled.Job.Queue);
        Assert.Equal("0 3 * * *", scheduled.CronExpression);
        Assert.Equal(typeof(RebuildDailyTotalsJob), scheduled.Job.Type);
        Assert.Equal(nameof(RebuildDailyTotalsJob.RebuildAsync), scheduled.Job.Method.Name);
        Assert.Empty(recurringJobManager.RemovedRecurringJobIds);
    }

    [Fact]
    public void Scheduled_jobs_always_run_on_utc_so_a_host_time_zone_cannot_shift_the_schedule()
    {
        var recurringJobManager = new RecordingRecurringJobManager();

        recurringJobManager.ScheduleRecurringJob<RebuildDailyTotalsJob>(
            RecurringJobId,
            new RecurringJobSettings { Enabled = true, Queue = "reporting", Cron = "0 3 * * *" },
            rebuildDailyTotalsJob => rebuildDailyTotalsJob.RebuildAsync());

        Assert.Equal(TimeZoneInfo.Utc, recurringJobManager.ScheduledJobs.Single().Options.TimeZone);
    }

    [Fact]
    public void Disabled_job_is_removed_from_storage_instead_of_scheduled()
    {
        var recurringJobManager = new RecordingRecurringJobManager();

        recurringJobManager.ScheduleRecurringJob<RebuildDailyTotalsJob>(
            RecurringJobId,
            new RecurringJobSettings { Enabled = false },
            rebuildDailyTotalsJob => rebuildDailyTotalsJob.RebuildAsync());

        Assert.Empty(recurringJobManager.ScheduledJobs);
        Assert.Equal([RecurringJobId], recurringJobManager.RemovedRecurringJobIds);
    }

    [Theory]
    [InlineData("", "0 3 * * *")]
    [InlineData("reporting", "")]
    public void Enabled_job_missing_its_queue_or_cron_fails_instead_of_scheduling_silently(
        string queue,
        string cron)
    {
        var recurringJobManager = new RecordingRecurringJobManager();

        Assert.Throws<ArgumentException>(
            () => recurringJobManager.ScheduleRecurringJob<RebuildDailyTotalsJob>(
                RecurringJobId,
                new RecurringJobSettings { Enabled = true, Queue = queue, Cron = cron },
                rebuildDailyTotalsJob => rebuildDailyTotalsJob.RebuildAsync()));

        Assert.Empty(recurringJobManager.ScheduledJobs);
    }

    private sealed class RebuildDailyTotalsJob
    {
        public Task RebuildAsync()
        {
            return Task.CompletedTask;
        }
    }

    private sealed record ScheduledRecurringJob(
        string RecurringJobId,
        Job Job,
        string CronExpression,
        RecurringJobOptions Options);

    private sealed class RecordingRecurringJobManager : IRecurringJobManager
    {
        private readonly List<ScheduledRecurringJob> scheduledJobs = [];
        private readonly List<string> removedRecurringJobIds = [];

        public IReadOnlyList<ScheduledRecurringJob> ScheduledJobs
        {
            get
            {
                return scheduledJobs;
            }
        }

        public IReadOnlyList<string> RemovedRecurringJobIds
        {
            get
            {
                return removedRecurringJobIds;
            }
        }

        public void AddOrUpdate(string recurringJobId, Job job, string cronExpression, RecurringJobOptions options)
        {
            scheduledJobs.Add(new ScheduledRecurringJob(recurringJobId, job, cronExpression, options));
        }

        public void AddOrUpdate(
            string recurringJobId,
            string queue,
            Job job,
            string cronExpression,
            RecurringJobOptions options)
        {
            scheduledJobs.Add(new ScheduledRecurringJob(recurringJobId, job, cronExpression, options));
        }

        public void RemoveIfExists(string recurringJobId)
        {
            removedRecurringJobIds.Add(recurringJobId);
        }

        public void Trigger(string recurringJobId)
        {
        }
    }
}
