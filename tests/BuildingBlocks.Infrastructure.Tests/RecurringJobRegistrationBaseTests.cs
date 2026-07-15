using Hangfire;
using Hangfire.Common;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class RecurringJobRegistrationBaseTests
{
    [Fact]
    public void Register_calls_add_or_update_when_options_are_enabled()
    {
        var recurringJobManager = new RecordingRecurringJobManager();
        var registration = new TestRecurringJobRegistration(
            recurringJobManager,
            new RecurringJobScheduleOptions
            {
                Enabled = true,
                Cron = "0 0 * * *"
            });

        registration.Register();

        Assert.True(registration.AddOrUpdateWasCalled);
        Assert.Null(recurringJobManager.RemovedRecurringJobId);
    }

    [Fact]
    public void Register_does_not_call_add_or_update_when_options_are_disabled()
    {
        var recurringJobManager = new RecordingRecurringJobManager();
        var registration = new TestRecurringJobRegistration(
            recurringJobManager,
            new RecurringJobScheduleOptions
            {
                Enabled = false
            });

        registration.Register();

        Assert.False(registration.AddOrUpdateWasCalled);
    }

    [Fact]
    public void Register_removes_existing_job_when_options_are_disabled()
    {
        var recurringJobManager = new RecordingRecurringJobManager();
        var registration = new TestRecurringJobRegistration(
            recurringJobManager,
            new RecurringJobScheduleOptions
            {
                Enabled = false
            });

        registration.Register();

        Assert.Equal(TestRecurringJobRegistration.TestJobId, recurringJobManager.RemovedRecurringJobId);
    }

    [Fact]
    public void Create_utc_recurring_job_options_uses_utc_timezone()
    {
        var recurringJobOptions = TestRecurringJobRegistration.CreateOptions();

        Assert.Equal(TimeZoneInfo.Utc, recurringJobOptions.TimeZone);
    }

    private sealed class TestRecurringJobRegistration(
        IRecurringJobManager recurringJobManager,
        RecurringJobScheduleOptions options)
        : RecurringJobRegistrationBase<RecurringJobScheduleOptions>(recurringJobManager, options)
    {
        public const string TestJobId = "test-job";

        public bool AddOrUpdateWasCalled { get; private set; }

        protected override string JobId
        {
            get
            {
                return TestJobId;
            }
        }

        public static RecurringJobOptions CreateOptions()
        {
            return CreateUtcRecurringJobOptions();
        }

        protected override void AddOrUpdate()
        {
            AddOrUpdateWasCalled = true;
        }
    }

    private sealed class RecordingRecurringJobManager : IRecurringJobManager
    {
        public string? RemovedRecurringJobId { get; private set; }

        public void AddOrUpdate(string recurringJobId, Job job, string cronExpression, RecurringJobOptions options)
        {
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
