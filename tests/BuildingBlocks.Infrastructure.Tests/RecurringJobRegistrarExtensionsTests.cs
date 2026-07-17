using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class RecurringJobRegistrarExtensionsTests
{
    [Fact]
    public void Register_recurring_jobs_registers_every_registration_exactly_once()
    {
        var recorder = new DefinitionRecorder();
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddScoped<IRecurringJobDefinition>(
            _ => new RecordingRecurringJobDefinition("first-job", recorder));
        services.AddScoped<IRecurringJobDefinition>(
            _ => new RecordingRecurringJobDefinition("second-job", recorder));

        using var serviceProvider = services.BuildServiceProvider();
        serviceProvider.RegisterRecurringJobs();

        Assert.Equal(["first-job", "second-job"], recorder.RegisteredJobIds);
    }

    [Fact]
    public void Register_recurring_jobs_does_nothing_when_no_registrations_are_present()
    {
        using var serviceProvider = new ServiceCollection().BuildServiceProvider();

        serviceProvider.RegisterRecurringJobs();
    }

    private sealed class DefinitionRecorder
    {
        private readonly List<string> registeredJobIds = [];

        public IReadOnlyList<string> RegisteredJobIds
        {
            get
            {
                return registeredJobIds;
            }
        }

        public void Record(string jobId)
        {
            registeredJobIds.Add(jobId);
        }
    }

    private sealed class RecordingRecurringJobDefinition(string jobId, DefinitionRecorder recorder)
        : IRecurringJobDefinition
    {
        public void Register()
        {
            recorder.Record(jobId);
        }
    }
}
