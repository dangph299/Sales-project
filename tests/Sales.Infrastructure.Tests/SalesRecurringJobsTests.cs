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
    public void Maintenance_cleanup_definition_uses_expected_job_metadata()
    {
        var recurringJobManager = new RecordingRecurringJobManager();
        var definition = new MaintenanceCleanupJobDefinition(
            recurringJobManager,
            Options.Create(new SalesRecurringJobsOptions
            {
                MaintenanceCleanup = EnabledSchedule("0 0 * * *", HangfireQueueNames.Maintenance)
            }));

        definition.Register();

        Assert.Equal(SalesRecurringJobIds.MaintenanceCleanup, recurringJobManager.AddedRecurringJobId);
        Assert.Equal(HangfireQueueNames.Maintenance, recurringJobManager.AddedJob?.Queue);
        Assert.Equal("0 0 * * *", recurringJobManager.AddedCronExpression);
        Assert.Equal(typeof(MaintenanceCleanupJob), recurringJobManager.AddedJob?.Type);
        Assert.Equal(nameof(MaintenanceCleanupJob.CleanupAsync), recurringJobManager.AddedJob?.Method.Name);
        Assert.Equal(TimeZoneInfo.Utc, recurringJobManager.AddedOptions?.TimeZone);
    }

    [Fact]
    public void Cancel_expired_pending_orders_definition_uses_expected_job_metadata_and_parameters()
    {
        var recurringJobManager = new RecordingRecurringJobManager();
        var definition = new CancelExpiredPendingOrdersJobDefinition(
            recurringJobManager,
            Options.Create(new SalesRecurringJobsOptions
            {
                CancelExpiredPendingOrders = new CancelExpiredPendingOrdersJobOptions
                {
                    Schedule = EnabledSchedule("*/5 * * * *", HangfireQueueNames.Critical),
                    ExpirationMinutes = 45,
                    BatchSize = 75
                }
            }));

        definition.Register();

        Assert.Equal(SalesRecurringJobIds.CancelExpiredPendingOrders, recurringJobManager.AddedRecurringJobId);
        Assert.Equal(HangfireQueueNames.Critical, recurringJobManager.AddedJob?.Queue);
        Assert.Equal("*/5 * * * *", recurringJobManager.AddedCronExpression);
        Assert.Equal(typeof(CancelExpiredPendingOrdersJob), recurringJobManager.AddedJob?.Type);
        Assert.Equal(nameof(CancelExpiredPendingOrdersJob.ExecuteAsync), recurringJobManager.AddedJob?.Method.Name);
        Assert.Equal(45, recurringJobManager.AddedJob?.Args[0]);
        Assert.Equal(75, recurringJobManager.AddedJob?.Args[1]);
        Assert.Equal(TimeZoneInfo.Utc, recurringJobManager.AddedOptions?.TimeZone);
    }

    [Fact]
    public void Definitions_take_the_queue_from_configuration()
    {
        var recurringJobManager = new RecordingRecurringJobManager();
        var definition = new CancelExpiredPendingOrdersJobDefinition(
            recurringJobManager,
            Options.Create(new SalesRecurringJobsOptions
            {
                CancelExpiredPendingOrders = new CancelExpiredPendingOrdersJobOptions
                {
                    Schedule = EnabledSchedule("*/5 * * * *", "custom-queue")
                }
            }));

        definition.Register();

        Assert.Equal("custom-queue", recurringJobManager.AddedJob?.Queue);
    }

    [Fact]
    public void Disabled_definition_removes_existing_job_without_add_or_update()
    {
        var recurringJobManager = new RecordingRecurringJobManager();
        var definition = new CancelExpiredPendingOrdersJobDefinition(
            recurringJobManager,
            Options.Create(new SalesRecurringJobsOptions
            {
                CancelExpiredPendingOrders = new CancelExpiredPendingOrdersJobOptions
                {
                    Schedule = new RecurringJobSettings { Enabled = false }
                }
            }));

        definition.Register();

        Assert.Null(recurringJobManager.AddedRecurringJobId);
        Assert.Equal(SalesRecurringJobIds.CancelExpiredPendingOrders, recurringJobManager.RemovedRecurringJobId);
    }

    [Fact]
    public void Sales_recurring_jobs_are_registered_as_independent_definitions()
    {
        var services = CreateServices(EnabledConfiguration());

        using var serviceProvider = services.BuildServiceProvider();
        var definitions = serviceProvider.GetServices<IRecurringJobDefinition>().ToArray();

        Assert.Equal(2, definitions.Length);
        Assert.Single(definitions, definition => definition is MaintenanceCleanupJobDefinition);
        Assert.Single(definitions, definition => definition is CancelExpiredPendingOrdersJobDefinition);
    }

    [Fact]
    public void Sales_registers_each_recurring_job_id_exactly_once()
    {
        var recurringJobManager = new RecordingRecurringJobManager();
        var services = CreateServices(EnabledConfiguration(), recurringJobManager);

        using var serviceProvider = services.BuildServiceProvider();
        serviceProvider.RegisterRecurringJobs();

        Assert.Equal(
            [SalesRecurringJobIds.CancelExpiredPendingOrders, SalesRecurringJobIds.MaintenanceCleanup],
            recurringJobManager.AddedRecurringJobIds.Order());
    }

    [Fact]
    public void Sales_recurring_job_ids_keep_their_existing_values()
    {
        Assert.Equal("sales-cleanup", SalesRecurringJobIds.MaintenanceCleanup);
        Assert.Equal("orders:cancel-expired", SalesRecurringJobIds.CancelExpiredPendingOrders);
    }

    [Fact]
    public void Job_id_is_not_configurable_so_configuration_cannot_fork_a_second_job()
    {
        var recurringJobManager = new RecordingRecurringJobManager();
        var configurationValues = EnabledConfiguration();
        configurationValues["SalesRecurringJobs:CancelExpiredPendingOrders:Schedule:JobId"] = "rogue-job-id";
        var services = CreateServices(configurationValues, recurringJobManager);

        using var serviceProvider = services.BuildServiceProvider();
        serviceProvider.RegisterRecurringJobs();

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
            ["SalesRecurringJobs:CancelExpiredPendingOrders:ExpirationMinutes"] = "0"
        });

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<SalesRecurringJobsOptions>>().Value;

        Assert.False(options.MaintenanceCleanup.Enabled);
        Assert.False(options.CancelExpiredPendingOrders.Schedule.Enabled);
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
            ["SalesRecurringJobs:CancelExpiredPendingOrders:BatchSize"] = "75"
        };
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

    private sealed class RecordingRecurringJobManager : IRecurringJobManager
    {
        private readonly List<string> addedRecurringJobIds = [];

        public IReadOnlyList<string> AddedRecurringJobIds
        {
            get
            {
                return addedRecurringJobIds;
            }
        }

        public string? AddedRecurringJobId { get; private set; }

        public string? AddedQueue { get; private set; }

        public Job? AddedJob { get; private set; }

        public string? AddedCronExpression { get; private set; }

        public RecurringJobOptions? AddedOptions { get; private set; }

        public string? RemovedRecurringJobId { get; private set; }

        public void AddOrUpdate(string recurringJobId, Job job, string cronExpression, RecurringJobOptions options)
        {
            addedRecurringJobIds.Add(recurringJobId);
            AddedRecurringJobId = recurringJobId;
            AddedJob = job;
            AddedCronExpression = cronExpression;
            AddedOptions = options;
        }

        public void AddOrUpdate(
            string recurringJobId,
            string queue,
            Job job,
            string cronExpression,
            RecurringJobOptions options)
        {
            addedRecurringJobIds.Add(recurringJobId);
            AddedRecurringJobId = recurringJobId;
            AddedQueue = queue;
            AddedJob = job;
            AddedCronExpression = cronExpression;
            AddedOptions = options;
        }

        public void RemoveIfExists(string recurringJobId)
        {
            RemovedRecurringJobId = recurringJobId;
        }

        public void Trigger(string recurringJobId)
        {
        }
    }
}
