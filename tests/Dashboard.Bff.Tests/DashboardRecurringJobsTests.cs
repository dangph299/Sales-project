using BuildingBlocks.Infrastructure;
using Dashboard.Bff.Extensions;
using Dashboard.Bff.Jobs;
using Dashboard.Bff.Options;
using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dashboard.Bff.Tests;

public sealed class DashboardRecurringJobsTests
{
    [Fact]
    public void Refresh_job_options_accept_valid_enabled_settings()
    {
        var options = new DashboardRefreshJobOptions
        {
            Enabled = true,
            Cron = "* * * * *",
            Queue = HangfireQueueNames.Default
        };

        Assert.True(options.IsValid());
    }

    [Theory]
    [InlineData("not-a-cron", "default")]
    [InlineData("* * * * *", "")]
    public void Refresh_job_options_reject_invalid_enabled_settings(string cron, string queue)
    {
        var options = new DashboardRefreshJobOptions
        {
            Enabled = true,
            Cron = cron,
            Queue = queue
        };

        Assert.False(options.IsValid());
    }

    [Fact]
    public void Disabled_refresh_job_does_not_require_schedule_settings()
    {
        var options = new DashboardRefreshJobOptions
        {
            Enabled = false,
            Cron = "",
            Queue = ""
        };

        Assert.True(options.IsValid());
    }

    [Fact]
    public void Snapshot_refresh_is_scheduled_with_expected_job_metadata()
    {
        var recurringJobManager = RegisterJobs(new DashboardRefreshJobOptions
        {
            Enabled = true,
            Cron = "* * * * *",
            Queue = HangfireQueueNames.Default
        });

        var refresh = recurringJobManager.Added(DashboardRecurringJobIds.SnapshotRefresh);

        Assert.NotNull(refresh);
        Assert.Equal(HangfireQueueNames.Default, refresh.Job.Queue);
        Assert.Equal("* * * * *", refresh.CronExpression);
        Assert.Equal(typeof(DashboardSnapshotRefreshJob), refresh.Job.Type);
        Assert.Equal(nameof(DashboardSnapshotRefreshJob.ExecuteAsync), refresh.Job.Method.Name);
        Assert.Equal(TimeZoneInfo.Utc, refresh.Options.TimeZone);
    }

    [Fact]
    public void Disabled_snapshot_refresh_is_removed_instead_of_scheduled()
    {
        var recurringJobManager = RegisterJobs(new DashboardRefreshJobOptions
        {
            Enabled = false
        });

        Assert.Null(recurringJobManager.Added(DashboardRecurringJobIds.SnapshotRefresh));
        Assert.Contains(DashboardRecurringJobIds.SnapshotRefresh, recurringJobManager.RemovedRecurringJobIds);
    }

    private static RecordingRecurringJobManager RegisterJobs(DashboardRefreshJobOptions options)
    {
        var recurringJobManager = new RecordingRecurringJobManager();
        var services = new ServiceCollection();
        services.AddSingleton<IRecurringJobManager>(recurringJobManager);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

        using var serviceProvider = services.BuildServiceProvider();
        serviceProvider.RegisterDashboardRecurringJobs();

        return recurringJobManager;
    }

    private sealed record ScheduledRecurringJob(
        string RecurringJobId,
        Job Job,
        string CronExpression,
        RecurringJobOptions Options);

    private sealed class RecordingRecurringJobManager : IRecurringJobManager
    {
        private readonly List<ScheduledRecurringJob> addedRecurringJobs = [];
        private readonly List<string> removedRecurringJobIds = [];

        public IReadOnlyList<string> RemovedRecurringJobIds => removedRecurringJobIds;

        public ScheduledRecurringJob? Added(string recurringJobId)
        {
            return addedRecurringJobs.SingleOrDefault(
                addedRecurringJob => addedRecurringJob.RecurringJobId == recurringJobId);
        }

        public void AddOrUpdate(string recurringJobId, Job job, string cronExpression, RecurringJobOptions options)
        {
            addedRecurringJobs.Add(new ScheduledRecurringJob(recurringJobId, job, cronExpression, options));
        }

        public void AddOrUpdate(
            string recurringJobId,
            string queue,
            Job job,
            string cronExpression,
            RecurringJobOptions options)
        {
            addedRecurringJobs.Add(new ScheduledRecurringJob(recurringJobId, job, cronExpression, options));
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
