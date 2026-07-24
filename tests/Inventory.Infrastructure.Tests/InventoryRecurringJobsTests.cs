using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Inventory.Infrastructure.Tests;

public sealed class InventoryRecurringJobsTests
{
    [Fact]
    public void Inventory_recurring_jobs_bind_the_root_configuration_section()
    {
        var services = CreateServices(EnabledConfiguration());

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<InventoryRecurringJobsOptions>>().Value;

        Assert.Equal("*/15 * * * *", options.ReplayDeadLetter.Schedule.Cron);
        Assert.Equal(HangfireQueueNames.Maintenance, options.ReplayDeadLetter.Schedule.Queue);
        Assert.Equal(25, options.ReplayDeadLetter.BatchSize);
        Assert.Equal(10, options.ReplayDeadLetter.RetryDelaySeconds);
        Assert.Equal(KafkaConsumerGroups.InventoryOrders, options.KafkaLagMonitor.GroupId);
        Assert.Equal([KafkaTopics.OrderConfirmationRequested, KafkaTopics.OrderUndoConfirmationRequested], options.KafkaLagMonitor.Topics);
        Assert.Equal(250, options.KafkaLagMonitor.WarningThreshold);
        Assert.Equal(20, options.KafkaLagMonitor.RequestTimeoutSeconds);
        Assert.Equal(300, options.InboxCleanup.BatchSize);
        Assert.Equal(21, options.InboxCleanup.RetentionDays);
        Assert.Equal(30, options.FailedOutboxRetry.BatchSize);
        Assert.Equal(5, options.FailedOutboxRetry.RetryDelaySeconds);
        Assert.Equal(500, options.OutboxPendingMonitor.BacklogWarningThreshold);
        Assert.Equal(600, options.OutboxPendingMonitor.OldestPendingWarningSeconds);
    }

    [Theory]
    [InlineData(InventoryRecurringJobIds.ReplayDeadLetter, typeof(ReplayDeadLetterJob), nameof(ReplayDeadLetterJob.ExecuteAsync), "*/15 * * * *")]
    [InlineData(InventoryRecurringJobIds.KafkaLagMonitor, typeof(KafkaLagMonitorJob), nameof(KafkaLagMonitorJob.ExecuteAsync), "*/5 * * * *")]
    [InlineData(InventoryRecurringJobIds.InboxCleanup, typeof(InboxCleanupJob), nameof(InboxCleanupJob.ExecuteAsync), "0 1 * * *")]
    [InlineData(InventoryRecurringJobIds.FailedOutboxRetry, typeof(FailedOutboxRetryJob), nameof(FailedOutboxRetryJob.ExecuteAsync), "*/20 * * * *")]
    [InlineData(InventoryRecurringJobIds.OutboxPendingMonitor, typeof(OutboxPendingMonitorJob), nameof(OutboxPendingMonitorJob.ExecuteAsync), "*/3 * * * *")]
    public void Inventory_messaging_jobs_are_scheduled_with_expected_metadata(
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
    public void Inventory_registers_each_recurring_job_id_exactly_once()
    {
        var recurringJobManager = RegisterJobs(EnabledConfiguration());

        var expected = new[]
        {
            InventoryRecurringJobIds.FailedOutboxRetry,
            InventoryRecurringJobIds.InboxCleanup,
            InventoryRecurringJobIds.KafkaLagMonitor,
            InventoryRecurringJobIds.OutboxPendingMonitor,
            InventoryRecurringJobIds.ReplayDeadLetter
        };

        Assert.Equal(
            expected.Order(),
            recurringJobManager.AddedRecurringJobIds.Order());
    }

    [Fact]
    public void Inventory_recurring_job_ids_keep_their_expected_values()
    {
        Assert.Equal("inventory-dead-letter-replay", InventoryRecurringJobIds.ReplayDeadLetter);
        Assert.Equal("inventory-kafka-lag-monitor", InventoryRecurringJobIds.KafkaLagMonitor);
        Assert.Equal("inventory-inbox-cleanup", InventoryRecurringJobIds.InboxCleanup);
        Assert.Equal("inventory-failed-outbox-retry", InventoryRecurringJobIds.FailedOutboxRetry);
        Assert.Equal("inventory-outbox-pending-monitor", InventoryRecurringJobIds.OutboxPendingMonitor);
    }

    [Theory]
    [InlineData("InventoryRecurringJobs:ReplayDeadLetter:BatchSize", "0")]
    [InlineData("InventoryRecurringJobs:ReplayDeadLetter:RetryDelaySeconds", "-1")]
    [InlineData("InventoryRecurringJobs:KafkaLagMonitor:GroupId", "")]
    [InlineData("InventoryRecurringJobs:KafkaLagMonitor:Topics:0", "")]
    [InlineData("InventoryRecurringJobs:KafkaLagMonitor:RequestTimeoutSeconds", "0")]
    [InlineData("InventoryRecurringJobs:InboxCleanup:RetentionDays", "0")]
    [InlineData("InventoryRecurringJobs:InboxCleanup:BatchSize", "0")]
    [InlineData("InventoryRecurringJobs:FailedOutboxRetry:BatchSize", "0")]
    [InlineData("InventoryRecurringJobs:FailedOutboxRetry:RetryDelaySeconds", "-1")]
    [InlineData("InventoryRecurringJobs:OutboxPendingMonitor:BacklogWarningThreshold", "-1")]
    [InlineData("InventoryRecurringJobs:OutboxPendingMonitor:OldestPendingWarningSeconds", "-1")]
    public void Invalid_settings_fail_validation_while_the_job_is_enabled(string key, string invalidValue)
    {
        var configurationValues = EnabledConfiguration();
        configurationValues[key] = invalidValue;
        var services = CreateServices(configurationValues);

        using var serviceProvider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IOptions<InventoryRecurringJobsOptions>>().Value);
    }

    [Fact]
    public void Disabled_jobs_do_not_require_their_settings()
    {
        var services = CreateServices(new Dictionary<string, string?>
        {
            ["InventoryRecurringJobs:ReplayDeadLetter:Schedule:Enabled"] = "false",
            ["InventoryRecurringJobs:ReplayDeadLetter:BatchSize"] = "0",
            ["InventoryRecurringJobs:KafkaLagMonitor:Schedule:Enabled"] = "false",
            ["InventoryRecurringJobs:KafkaLagMonitor:RequestTimeoutSeconds"] = "0",
            ["InventoryRecurringJobs:InboxCleanup:Schedule:Enabled"] = "false",
            ["InventoryRecurringJobs:InboxCleanup:RetentionDays"] = "0",
            ["InventoryRecurringJobs:FailedOutboxRetry:Schedule:Enabled"] = "false",
            ["InventoryRecurringJobs:FailedOutboxRetry:BatchSize"] = "0",
            ["InventoryRecurringJobs:OutboxPendingMonitor:Schedule:Enabled"] = "false",
            ["InventoryRecurringJobs:OutboxPendingMonitor:BacklogWarningThreshold"] = "-1"
        });

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<InventoryRecurringJobsOptions>>().Value;

        Assert.False(options.ReplayDeadLetter.Schedule.Enabled);
        Assert.False(options.KafkaLagMonitor.Schedule.Enabled);
        Assert.False(options.InboxCleanup.Schedule.Enabled);
        Assert.False(options.FailedOutboxRetry.Schedule.Enabled);
        Assert.False(options.OutboxPendingMonitor.Schedule.Enabled);
    }

    private static Dictionary<string, string?> EnabledConfiguration()
    {
        return new Dictionary<string, string?>
        {
            ["InventoryRecurringJobs:ReplayDeadLetter:Schedule:Enabled"] = "true",
            ["InventoryRecurringJobs:ReplayDeadLetter:Schedule:Queue"] = HangfireQueueNames.Maintenance,
            ["InventoryRecurringJobs:ReplayDeadLetter:Schedule:Cron"] = "*/15 * * * *",
            ["InventoryRecurringJobs:ReplayDeadLetter:BatchSize"] = "25",
            ["InventoryRecurringJobs:ReplayDeadLetter:RetryDelaySeconds"] = "10",
            ["InventoryRecurringJobs:KafkaLagMonitor:Schedule:Enabled"] = "true",
            ["InventoryRecurringJobs:KafkaLagMonitor:Schedule:Queue"] = HangfireQueueNames.Maintenance,
            ["InventoryRecurringJobs:KafkaLagMonitor:Schedule:Cron"] = "*/5 * * * *",
            ["InventoryRecurringJobs:KafkaLagMonitor:GroupId"] = KafkaConsumerGroups.InventoryOrders,
            ["InventoryRecurringJobs:KafkaLagMonitor:Topics:0"] = KafkaTopics.OrderConfirmationRequested,
            ["InventoryRecurringJobs:KafkaLagMonitor:Topics:1"] = KafkaTopics.OrderUndoConfirmationRequested,
            ["InventoryRecurringJobs:KafkaLagMonitor:WarningThreshold"] = "250",
            ["InventoryRecurringJobs:KafkaLagMonitor:RequestTimeoutSeconds"] = "20",
            ["InventoryRecurringJobs:InboxCleanup:Schedule:Enabled"] = "true",
            ["InventoryRecurringJobs:InboxCleanup:Schedule:Queue"] = HangfireQueueNames.Maintenance,
            ["InventoryRecurringJobs:InboxCleanup:Schedule:Cron"] = "0 1 * * *",
            ["InventoryRecurringJobs:InboxCleanup:BatchSize"] = "300",
            ["InventoryRecurringJobs:InboxCleanup:RetentionDays"] = "21",
            ["InventoryRecurringJobs:FailedOutboxRetry:Schedule:Enabled"] = "true",
            ["InventoryRecurringJobs:FailedOutboxRetry:Schedule:Queue"] = HangfireQueueNames.Maintenance,
            ["InventoryRecurringJobs:FailedOutboxRetry:Schedule:Cron"] = "*/20 * * * *",
            ["InventoryRecurringJobs:FailedOutboxRetry:BatchSize"] = "30",
            ["InventoryRecurringJobs:FailedOutboxRetry:RetryDelaySeconds"] = "5",
            ["InventoryRecurringJobs:OutboxPendingMonitor:Schedule:Enabled"] = "true",
            ["InventoryRecurringJobs:OutboxPendingMonitor:Schedule:Queue"] = HangfireQueueNames.Maintenance,
            ["InventoryRecurringJobs:OutboxPendingMonitor:Schedule:Cron"] = "*/3 * * * *",
            ["InventoryRecurringJobs:OutboxPendingMonitor:BacklogWarningThreshold"] = "500",
            ["InventoryRecurringJobs:OutboxPendingMonitor:OldestPendingWarningSeconds"] = "600"
        };
    }

    private static RecordingRecurringJobManager RegisterJobs(IDictionary<string, string?> configurationValues)
    {
        var recurringJobManager = new RecordingRecurringJobManager();
        var services = CreateServices(configurationValues, recurringJobManager);

        using var serviceProvider = services.BuildServiceProvider();
        serviceProvider.RegisterInventoryRecurringJobs();

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
        services.AddInventoryRecurringJobs(configuration);
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
