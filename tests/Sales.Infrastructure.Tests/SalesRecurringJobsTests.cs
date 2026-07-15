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
    public void Sales_recurring_jobs_bind_expected_sections()
    {
        var services = CreateServices(new Dictionary<string, string?>
        {
            ["SalesRecurringJobs:MaintenanceCleanup:Enabled"] = "true",
            ["SalesRecurringJobs:MaintenanceCleanup:Cron"] = "0 0 * * *",
            ["SalesRecurringJobs:CancelExpiredPendingOrders:Enabled"] = "true",
            ["SalesRecurringJobs:CancelExpiredPendingOrders:Cron"] = "*/5 * * * *",
            ["SalesRecurringJobs:CancelExpiredPendingOrders:ExpirationMinutes"] = "45",
            ["SalesRecurringJobs:CancelExpiredPendingOrders:BatchSize"] = "75"
        });

        using var serviceProvider = services.BuildServiceProvider();
        var scheduleOptions = serviceProvider.GetRequiredService<IOptionsMonitor<RecurringJobScheduleOptions>>();
        var cancelOptions = serviceProvider.GetRequiredService<IOptions<CancelExpiredPendingOrdersJobOptions>>().Value;

        Assert.Equal("0 0 * * *", scheduleOptions.Get(MaintenanceCleanupRecurringJobRegistration.OptionsName).Cron);
        Assert.Equal("*/5 * * * *", cancelOptions.Cron);
        Assert.Equal(45, cancelOptions.ExpirationMinutes);
        Assert.Equal(75, cancelOptions.BatchSize);
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
            Enabled = true,
            Cron = "*/5 * * * *",
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
            Enabled = false,
            ExpirationMinutes = 0,
            BatchSize = 0
        };

        Assert.True(options.IsValid());
    }

    [Fact]
    public void Maintenance_cleanup_registration_uses_expected_job_metadata()
    {
        var recurringJobManager = new RecordingRecurringJobManager();
        var options = new TestOptionsMonitor<RecurringJobScheduleOptions>(
            MaintenanceCleanupRecurringJobRegistration.OptionsName,
            new RecurringJobScheduleOptions
            {
                Enabled = true,
                Cron = "0 0 * * *"
            });
        var registration = new MaintenanceCleanupRecurringJobRegistration(recurringJobManager, options);

        registration.Register();

        Assert.Equal(SalesRecurringJobIds.MaintenanceCleanup, recurringJobManager.AddedRecurringJobId);
        Assert.Equal(HangfireQueueNames.Maintenance, recurringJobManager.AddedJob?.Queue);
        Assert.Equal("0 0 * * *", recurringJobManager.AddedCronExpression);
        Assert.Equal(typeof(MaintenanceJobs), recurringJobManager.AddedJob?.Type);
        Assert.Equal(nameof(MaintenanceJobs.CleanupAsync), recurringJobManager.AddedJob?.Method.Name);
        Assert.Equal(TimeZoneInfo.Utc, recurringJobManager.AddedOptions?.TimeZone);
    }

    [Fact]
    public void Cancel_expired_pending_orders_registration_uses_expected_job_metadata_and_parameters()
    {
        var recurringJobManager = new RecordingRecurringJobManager();
        var registration = new CancelExpiredPendingOrdersRecurringJobRegistration(
            recurringJobManager,
            Options.Create(new CancelExpiredPendingOrdersJobOptions
            {
                Enabled = true,
                Cron = "*/5 * * * *",
                ExpirationMinutes = 45,
                BatchSize = 75
            }));

        registration.Register();

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
    public void Disabled_registration_removes_existing_job_without_add_or_update()
    {
        var recurringJobManager = new RecordingRecurringJobManager();
        var registration = new CancelExpiredPendingOrdersRecurringJobRegistration(
            recurringJobManager,
            Options.Create(new CancelExpiredPendingOrdersJobOptions
            {
                Enabled = false
            }));

        registration.Register();

        Assert.Null(recurringJobManager.AddedRecurringJobId);
        Assert.Equal(SalesRecurringJobIds.CancelExpiredPendingOrders, recurringJobManager.RemovedRecurringJobId);
    }

    [Fact]
    public void Sales_recurring_jobs_are_registered_as_independent_registrations()
    {
        var services = CreateServices(new Dictionary<string, string?>
        {
            ["SalesRecurringJobs:MaintenanceCleanup:Enabled"] = "true",
            ["SalesRecurringJobs:MaintenanceCleanup:Cron"] = "0 0 * * *",
            ["SalesRecurringJobs:CancelExpiredPendingOrders:Enabled"] = "true",
            ["SalesRecurringJobs:CancelExpiredPendingOrders:Cron"] = "*/5 * * * *"
        });

        using var serviceProvider = services.BuildServiceProvider();
        var registrations = serviceProvider.GetServices<IRecurringJobRegistration>().ToArray();

        Assert.Contains(registrations, registration => registration is MaintenanceCleanupRecurringJobRegistration);
        Assert.Contains(registrations, registration => registration is CancelExpiredPendingOrdersRecurringJobRegistration);
    }

    private static IServiceCollection CreateServices(IDictionary<string, string?> configurationValues)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IRecurringJobManager, RecordingRecurringJobManager>();
        services.AddSalesRecurringJobs(configuration);
        return services;
    }

    private sealed class RecordingRecurringJobManager : IRecurringJobManager
    {
        public string? AddedRecurringJobId { get; private set; }

        public string? AddedQueue { get; private set; }

        public Job? AddedJob { get; private set; }

        public string? AddedCronExpression { get; private set; }

        public RecurringJobOptions? AddedOptions { get; private set; }

        public string? RemovedRecurringJobId { get; private set; }

        public void AddOrUpdate(string recurringJobId, Job job, string cronExpression, RecurringJobOptions options)
        {
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

    private sealed class TestOptionsMonitor<TOptions>(string optionsName, TOptions options) : IOptionsMonitor<TOptions>
    {
        public TOptions CurrentValue
        {
            get
            {
                return options;
            }
        }

        public TOptions Get(string? name)
        {
            if (name == optionsName)
            {
                return options;
            }

            throw new InvalidOperationException($"Unexpected options name '{name}'.");
        }

        public IDisposable? OnChange(Action<TOptions, string?> listener)
        {
            return null;
        }
    }
}
