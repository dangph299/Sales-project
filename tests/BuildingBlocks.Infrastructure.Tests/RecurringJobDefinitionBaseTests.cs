using Hangfire;
using Hangfire.Common;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class RecurringJobDefinitionBaseTests
{
    [Fact]
    public void Register_calls_add_or_update_when_settings_are_enabled()
    {
        var recurringJobManager = new RecordingRecurringJobManager();
        var definition = new TestRecurringJobDefinition(
            recurringJobManager,
            new RecurringJobSettings
            {
                Enabled = true,
                Cron = "0 0 * * *"
            });

        definition.Register();

        Assert.True(definition.AddOrUpdateWasCalled);
        Assert.Null(recurringJobManager.RemovedRecurringJobId);
    }

    [Fact]
    public void Register_does_not_call_add_or_update_when_settings_are_disabled()
    {
        var recurringJobManager = new RecordingRecurringJobManager();
        var definition = new TestRecurringJobDefinition(
            recurringJobManager,
            new RecurringJobSettings
            {
                Enabled = false
            });

        definition.Register();

        Assert.False(definition.AddOrUpdateWasCalled);
    }

    [Fact]
    public void Register_removes_existing_job_when_settings_are_disabled()
    {
        var recurringJobManager = new RecordingRecurringJobManager();
        var definition = new TestRecurringJobDefinition(
            recurringJobManager,
            new RecurringJobSettings
            {
                Enabled = false
            });

        definition.Register();

        Assert.Equal(TestRecurringJobDefinition.TestJobId, recurringJobManager.RemovedRecurringJobId);
    }

    private sealed class TestRecurringJobDefinition(
        IRecurringJobManager recurringJobManager,
        RecurringJobSettings settings)
        : RecurringJobDefinitionBase(recurringJobManager, settings)
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
