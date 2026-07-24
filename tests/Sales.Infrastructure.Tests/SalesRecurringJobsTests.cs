using BuildingBlocks.Infrastructure;
using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Sales.Infrastructure.Tests;

public sealed class SalesRecurringJobsTests
{
    [Fact]
    public void Sales_recurring_jobs_bind_the_root_configuration_section()
    {
        var services = CreateServices(EnabledConfiguration());

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<SalesRecurringJobsOptions>>().Value;

        Assert.Equal("0 0 * * *", options.MaintenanceCleanup.Cron);
        Assert.Equal(HangfireQueueNames.Maintenance, options.MaintenanceCleanup.Queue);
        Assert.Equal("*/5 * * * *", options.CancelExpiredPendingOrders.Schedule.Cron);
        Assert.Equal(HangfireQueueNames.Critical, options.CancelExpiredPendingOrders.Schedule.Queue);
        Assert.Equal(45, options.CancelExpiredPendingOrders.ExpirationMinutes);
        Assert.Equal(75, options.CancelExpiredPendingOrders.BatchSize);
        Assert.Equal("*/15 * * * *", options.ReplayDeadLetter.Schedule.Cron);
        Assert.Equal(HangfireQueueNames.Maintenance, options.ReplayDeadLetter.Schedule.Queue);
        Assert.Equal(25, options.ReplayDeadLetter.BatchSize);
        Assert.Equal(10, options.ReplayDeadLetter.RetryDelaySeconds);
        Assert.Equal("*/5 * * * *", options.KafkaLagMonitor.Schedule.Cron);
        Assert.Equal(HangfireQueueNames.Maintenance, options.KafkaLagMonitor.Schedule.Queue);
        Assert.Equal(250, options.KafkaLagMonitor.WarningThreshold);
        Assert.Equal(20, options.KafkaLagMonitor.RequestTimeoutSeconds);
        Assert.Equal("0 1 * * *", options.InboxCleanup.Schedule.Cron);
        Assert.Equal(300, options.InboxCleanup.BatchSize);
        Assert.Equal(21, options.InboxCleanup.RetentionDays);
        Assert.Equal("*/20 * * * *", options.FailedOutboxRetry.Schedule.Cron);
        Assert.Equal(30, options.FailedOutboxRetry.BatchSize);
        Assert.Equal(5, options.FailedOutboxRetry.RetryDelaySeconds);
        Assert.Equal("*/3 * * * *", options.OutboxPendingMonitor.Schedule.Cron);
        Assert.Equal(500, options.OutboxPendingMonitor.BacklogWarningThreshold);
        Assert.Equal(600, options.OutboxPendingMonitor.OldestPendingWarningSeconds);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(30, 0)]
    public void Cancel_expired_pending_orders_options_reject_non_positive_business_parameters(
        int expirationMinutes,
        int batchSize)
    {
        var options = new CancelExpiredPendingOrdersJobOptions
        {
            Schedule = EnabledSchedule("*/5 * * * *", HangfireQueueNames.Critical),
            ExpirationMinutes = expirationMinutes,
            BatchSize = batchSize
        };

        Assert.False(options.IsValid());
    }

    [Fact]
    public void Cancel_expired_pending_orders_options_allow_disabled_job_without_business_parameters()
    {
        var options = new CancelExpiredPendingOrdersJobOptions
        {
            Schedule = new RecurringJobSettings { Enabled = false },
            ExpirationMinutes = 0,
            BatchSize = 0
        };

        Assert.True(options.IsValid());
    }

    [Fact]
    public void Maintenance_cleanup_is_scheduled_with_expected_job_metadata()
    {
        var recurringJobManager = RegisterJobs(EnabledConfiguration());

        var maintenanceCleanup = recurringJobManager.Added(SalesRecurringJobIds.MaintenanceCleanup);

        Assert.NotNull(maintenanceCleanup);
        Assert.Equal(HangfireQueueNames.Maintenance, maintenanceCleanup.Job.Queue);
        Assert.Equal("0 0 * * *", maintenanceCleanup.CronExpression);
        Assert.Equal(typeof(MaintenanceCleanupJob), maintenanceCleanup.Job.Type);
        Assert.Equal(nameof(MaintenanceCleanupJob.CleanupAsync), maintenanceCleanup.Job.Method.Name);
        Assert.Equal(TimeZoneInfo.Utc, maintenanceCleanup.Options.TimeZone);
    }

    [Fact]
    public void Cancel_expired_pending_orders_is_scheduled_with_expected_job_metadata_and_parameters()
    {
        var recurringJobManager = RegisterJobs(EnabledConfiguration());

        var cancelExpiredPendingOrders = recurringJobManager.Added(SalesRecurringJobIds.CancelExpiredPendingOrders);

        Assert.NotNull(cancelExpiredPendingOrders);
        Assert.Equal(HangfireQueueNames.Critical, cancelExpiredPendingOrders.Job.Queue);
        Assert.Equal("*/5 * * * *", cancelExpiredPendingOrders.CronExpression);
        Assert.Equal(typeof(CancelExpiredPendingOrdersJob), cancelExpiredPendingOrders.Job.Type);
        Assert.Equal(nameof(CancelExpiredPendingOrdersJob.ExecuteAsync), cancelExpiredPendingOrders.Job.Method.Name);
        Assert.Equal(45, cancelExpiredPendingOrders.Job.Args[0]);
        Assert.Equal(75, cancelExpiredPendingOrders.Job.Args[1]);
        Assert.Equal(TimeZoneInfo.Utc, cancelExpiredPendingOrders.Options.TimeZone);
    }

    [Theory]
    [InlineData(SalesRecurringJobIds.ReplayDeadLetter, typeof(ReplayDeadLetterJob), nameof(ReplayDeadLetterJob.ExecuteAsync), "*/15 * * * *")]
    [InlineData(SalesRecurringJobIds.KafkaLagMonitor, typeof(KafkaLagMonitorJob), nameof(KafkaLagMonitorJob.ExecuteAsync), "*/5 * * * *")]
    [InlineData(SalesRecurringJobIds.InboxCleanup, typeof(InboxCleanupJob), nameof(InboxCleanupJob.ExecuteAsync), "0 1 * * *")]
    [InlineData(SalesRecurringJobIds.FailedOutboxRetry, typeof(FailedOutboxRetryJob), nameof(FailedOutboxRetryJob.ExecuteAsync), "*/20 * * * *")]
    [InlineData(SalesRecurringJobIds.OutboxPendingMonitor, typeof(OutboxPendingMonitorJob), nameof(OutboxPendingMonitorJob.ExecuteAsync), "*/3 * * * *")]
    public void Messaging_reliability_jobs_are_scheduled_with_expected_metadata(
        string jobId,
        Type jobType,
        string methodName,
        string cron)
    {
        var recurringJobManager = RegisterJobs(EnabledConfiguration());

        var scheduled = recurringJobManager.Added(jobId);

        Assert.NotNull(scheduled);
        Assert.Equal(HangfireQueueNames.Maintenance, scheduled.Job.Queue);
        Assert.Equal(cron, scheduled.CronExpression);
        Assert.Equal(jobType, scheduled.Job.Type);
        Assert.Equal(methodName, scheduled.Job.Method.Name);
        Assert.Equal(TimeZoneInfo.Utc, scheduled.Options.TimeZone);
    }

    [Fact]
    public void Jobs_take_the_queue_from_configuration()
    {
        var configurationValues = EnabledConfiguration();
        configurationValues["SalesRecurringJobs:CancelExpiredPendingOrders:Schedule:Queue"] = "custom-queue";

        var recurringJobManager = RegisterJobs(configurationValues);

        Assert.Equal(
            "custom-queue",
            recurringJobManager.Added(SalesRecurringJobIds.CancelExpiredPendingOrders)?.Job.Queue);
    }

    [Fact]
    public void Disabled_job_is_removed_instead_of_scheduled()
    {
        var configurationValues = EnabledConfiguration();
        configurationValues["SalesRecurringJobs:CancelExpiredPendingOrders:Schedule:Enabled"] = "false";

        var recurringJobManager = RegisterJobs(configurationValues);

        Assert.Null(recurringJobManager.Added(SalesRecurringJobIds.CancelExpiredPendingOrders));
        Assert.Contains(SalesRecurringJobIds.CancelExpiredPendingOrders, recurringJobManager.RemovedRecurringJobIds);
    }

    [Fact]
    public void Sales_registers_each_recurring_job_id_exactly_once()
    {
        var recurringJobManager = RegisterJobs(EnabledConfiguration());

        Assert.Equal(
            [
                SalesRecurringJobIds.FailedOutboxRetry,
                SalesRecurringJobIds.InboxCleanup,
                SalesRecurringJobIds.KafkaLagMonitor,
                SalesRecurringJobIds.OutboxPendingMonitor,
                SalesRecurringJobIds.ReplayDeadLetter,
                SalesRecurringJobIds.CancelExpiredPendingOrders,
                SalesRecurringJobIds.MaintenanceCleanup
            ],
            recurringJobManager.AddedRecurringJobIds.Order());
    }

    [Fact]
    public void Sales_recurring_job_ids_keep_their_existing_values()
    {
        Assert.Equal("sales-cleanup", SalesRecurringJobIds.MaintenanceCleanup);
        Assert.Equal("orders:cancel-expired", SalesRecurringJobIds.CancelExpiredPendingOrders);
        Assert.Equal("messaging:replay-dead-letter", SalesRecurringJobIds.ReplayDeadLetter);
        Assert.Equal("messaging:kafka-lag-monitor", SalesRecurringJobIds.KafkaLagMonitor);
        Assert.Equal("messaging:inbox-cleanup", SalesRecurringJobIds.InboxCleanup);
        Assert.Equal("messaging:failed-outbox-retry", SalesRecurringJobIds.FailedOutboxRetry);
        Assert.Equal("messaging:outbox-pending-monitor", SalesRecurringJobIds.OutboxPendingMonitor);
    }

    [Fact]
    public void Job_id_is_not_configurable_so_configuration_cannot_fork_a_second_job()
    {
        var configurationValues = EnabledConfiguration();
        configurationValues["SalesRecurringJobs:CancelExpiredPendingOrders:Schedule:JobId"] = "rogue-job-id";

        var recurringJobManager = RegisterJobs(configurationValues);

        Assert.DoesNotContain("rogue-job-id", recurringJobManager.AddedRecurringJobIds);
        Assert.Contains(SalesRecurringJobIds.CancelExpiredPendingOrders, recurringJobManager.AddedRecurringJobIds);
    }

    [Theory]
    [InlineData("SalesRecurringJobs:MaintenanceCleanup:Cron", "not-a-cron")]
    [InlineData("SalesRecurringJobs:MaintenanceCleanup:Queue", "")]
    [InlineData("SalesRecurringJobs:CancelExpiredPendingOrders:Schedule:Cron", "not-a-cron")]
    [InlineData("SalesRecurringJobs:CancelExpiredPendingOrders:Schedule:Queue", "")]
    [InlineData("SalesRecurringJobs:CancelExpiredPendingOrders:ExpirationMinutes", "0")]
    [InlineData("SalesRecurringJobs:CancelExpiredPendingOrders:BatchSize", "0")]
    [InlineData("SalesRecurringJobs:ReplayDeadLetter:BatchSize", "0")]
    [InlineData("SalesRecurringJobs:ReplayDeadLetter:RetryDelaySeconds", "-1")]
    [InlineData("SalesRecurringJobs:KafkaLagMonitor:GroupId", "")]
    [InlineData("SalesRecurringJobs:KafkaLagMonitor:Topics:0", "")]
    [InlineData("SalesRecurringJobs:KafkaLagMonitor:RequestTimeoutSeconds", "0")]
    [InlineData("SalesRecurringJobs:InboxCleanup:RetentionDays", "0")]
    [InlineData("SalesRecurringJobs:InboxCleanup:BatchSize", "0")]
    [InlineData("SalesRecurringJobs:FailedOutboxRetry:BatchSize", "0")]
    [InlineData("SalesRecurringJobs:FailedOutboxRetry:RetryDelaySeconds", "-1")]
    [InlineData("SalesRecurringJobs:OutboxPendingMonitor:BacklogWarningThreshold", "-1")]
    [InlineData("SalesRecurringJobs:OutboxPendingMonitor:OldestPendingWarningSeconds", "-1")]
    public void Invalid_settings_fail_validation_while_the_job_is_enabled(string key, string invalidValue)
    {
        var configurationValues = EnabledConfiguration();
        configurationValues[key] = invalidValue;
        var services = CreateServices(configurationValues);

        using var serviceProvider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IOptions<SalesRecurringJobsOptions>>().Value);
    }

    [Fact]
    public void Disabled_jobs_do_not_require_their_settings()
    {
        var services = CreateServices(new Dictionary<string, string?>
        {
            ["SalesRecurringJobs:MaintenanceCleanup:Enabled"] = "false",
            ["SalesRecurringJobs:CancelExpiredPendingOrders:Schedule:Enabled"] = "false",
            ["SalesRecurringJobs:CancelExpiredPendingOrders:ExpirationMinutes"] = "0",
            ["SalesRecurringJobs:ReplayDeadLetter:Schedule:Enabled"] = "false",
            ["SalesRecurringJobs:ReplayDeadLetter:BatchSize"] = "0",
            ["SalesRecurringJobs:KafkaLagMonitor:Schedule:Enabled"] = "false",
            ["SalesRecurringJobs:KafkaLagMonitor:RequestTimeoutSeconds"] = "0",
            ["SalesRecurringJobs:InboxCleanup:Schedule:Enabled"] = "false",
            ["SalesRecurringJobs:InboxCleanup:RetentionDays"] = "0",
            ["SalesRecurringJobs:FailedOutboxRetry:Schedule:Enabled"] = "false",
            ["SalesRecurringJobs:FailedOutboxRetry:BatchSize"] = "0",
            ["SalesRecurringJobs:OutboxPendingMonitor:Schedule:Enabled"] = "false",
            ["SalesRecurringJobs:OutboxPendingMonitor:BacklogWarningThreshold"] = "-1"
        });

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<SalesRecurringJobsOptions>>().Value;

        Assert.False(options.MaintenanceCleanup.Enabled);
        Assert.False(options.CancelExpiredPendingOrders.Schedule.Enabled);
        Assert.False(options.ReplayDeadLetter.Schedule.Enabled);
        Assert.False(options.KafkaLagMonitor.Schedule.Enabled);
        Assert.False(options.InboxCleanup.Schedule.Enabled);
        Assert.False(options.FailedOutboxRetry.Schedule.Enabled);
        Assert.False(options.OutboxPendingMonitor.Schedule.Enabled);
    }

    private static RecurringJobSettings EnabledSchedule(string cron, string queue)
    {
        return new RecurringJobSettings
        {
            Enabled = true,
            Cron = cron,
            Queue = queue
        };
    }

    private static Dictionary<string, string?> EnabledConfiguration()
    {
        return new Dictionary<string, string?>
        {
            ["SalesRecurringJobs:MaintenanceCleanup:Enabled"] = "true",
            ["SalesRecurringJobs:MaintenanceCleanup:Queue"] = HangfireQueueNames.Maintenance,
            ["SalesRecurringJobs:MaintenanceCleanup:Cron"] = "0 0 * * *",
            ["SalesRecurringJobs:CancelExpiredPendingOrders:Schedule:Enabled"] = "true",
            ["SalesRecurringJobs:CancelExpiredPendingOrders:Schedule:Queue"] = HangfireQueueNames.Critical,
            ["SalesRecurringJobs:CancelExpiredPendingOrders:Schedule:Cron"] = "*/5 * * * *",
            ["SalesRecurringJobs:CancelExpiredPendingOrders:ExpirationMinutes"] = "45",
            ["SalesRecurringJobs:CancelExpiredPendingOrders:BatchSize"] = "75",
            ["SalesRecurringJobs:ReplayDeadLetter:Schedule:Enabled"] = "true",
            ["SalesRecurringJobs:ReplayDeadLetter:Schedule:Queue"] = HangfireQueueNames.Maintenance,
            ["SalesRecurringJobs:ReplayDeadLetter:Schedule:Cron"] = "*/15 * * * *",
            ["SalesRecurringJobs:ReplayDeadLetter:BatchSize"] = "25",
            ["SalesRecurringJobs:ReplayDeadLetter:RetryDelaySeconds"] = "10",
            ["SalesRecurringJobs:KafkaLagMonitor:Schedule:Enabled"] = "true",
            ["SalesRecurringJobs:KafkaLagMonitor:Schedule:Queue"] = HangfireQueueNames.Maintenance,
            ["SalesRecurringJobs:KafkaLagMonitor:Schedule:Cron"] = "*/5 * * * *",
            ["SalesRecurringJobs:KafkaLagMonitor:GroupId"] = "sales-inventory-results-v1",
            ["SalesRecurringJobs:KafkaLagMonitor:Topics:0"] = "inventory.stock-reserved.v1",
            ["SalesRecurringJobs:KafkaLagMonitor:WarningThreshold"] = "250",
            ["SalesRecurringJobs:KafkaLagMonitor:RequestTimeoutSeconds"] = "20",
            ["SalesRecurringJobs:InboxCleanup:Schedule:Enabled"] = "true",
            ["SalesRecurringJobs:InboxCleanup:Schedule:Queue"] = HangfireQueueNames.Maintenance,
            ["SalesRecurringJobs:InboxCleanup:Schedule:Cron"] = "0 1 * * *",
            ["SalesRecurringJobs:InboxCleanup:BatchSize"] = "300",
            ["SalesRecurringJobs:InboxCleanup:RetentionDays"] = "21",
            ["SalesRecurringJobs:FailedOutboxRetry:Schedule:Enabled"] = "true",
            ["SalesRecurringJobs:FailedOutboxRetry:Schedule:Queue"] = HangfireQueueNames.Maintenance,
            ["SalesRecurringJobs:FailedOutboxRetry:Schedule:Cron"] = "*/20 * * * *",
            ["SalesRecurringJobs:FailedOutboxRetry:BatchSize"] = "30",
            ["SalesRecurringJobs:FailedOutboxRetry:RetryDelaySeconds"] = "5",
            ["SalesRecurringJobs:OutboxPendingMonitor:Schedule:Enabled"] = "true",
            ["SalesRecurringJobs:OutboxPendingMonitor:Schedule:Queue"] = HangfireQueueNames.Maintenance,
            ["SalesRecurringJobs:OutboxPendingMonitor:Schedule:Cron"] = "*/3 * * * *",
            ["SalesRecurringJobs:OutboxPendingMonitor:BacklogWarningThreshold"] = "500",
            ["SalesRecurringJobs:OutboxPendingMonitor:OldestPendingWarningSeconds"] = "600"
        };
    }

    private static RecordingRecurringJobManager RegisterJobs(IDictionary<string, string?> configurationValues)
    {
        var recurringJobManager = new RecordingRecurringJobManager();
        var services = CreateServices(configurationValues, recurringJobManager);

        using var serviceProvider = services.BuildServiceProvider();
        serviceProvider.RegisterSalesRecurringJobs();

        return recurringJobManager;
    }

    private static IServiceCollection CreateServices(
        IDictionary<string, string?> configurationValues,
        RecordingRecurringJobManager? recurringJobManager = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IRecurringJobManager>(recurringJobManager ?? new RecordingRecurringJobManager());
        services.AddSalesRecurringJobs(configuration);
        return services;
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

        public IReadOnlyList<string> AddedRecurringJobIds
        {
            get
            {
                return addedRecurringJobs.Select(addedRecurringJob => addedRecurringJob.RecurringJobId).ToArray();
            }
        }

        public IReadOnlyList<string> RemovedRecurringJobIds
        {
            get
            {
                return removedRecurringJobIds;
            }
        }

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
