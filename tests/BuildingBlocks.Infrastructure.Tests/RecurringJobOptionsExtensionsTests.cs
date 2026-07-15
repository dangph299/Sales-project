using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class RecurringJobOptionsExtensionsTests
{
    [Fact]
    public void Named_options_bind_their_own_configuration_sections()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jobs:First:Enabled"] = "true",
                ["Jobs:First:Cron"] = "0 0 * * *",
                ["Jobs:Second:Enabled"] = "true",
                ["Jobs:Second:Cron"] = "*/10 * * * *"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddRecurringJobOptions<RecurringJobScheduleOptions>(
            configuration,
            "FirstJob",
            "Jobs:First",
            "First job options are invalid.");
        services.AddRecurringJobOptions<RecurringJobScheduleOptions>(
            configuration,
            "SecondJob",
            "Jobs:Second",
            "Second job options are invalid.");

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<RecurringJobScheduleOptions>>();

        Assert.Equal("0 0 * * *", options.Get("FirstJob").Cron);
        Assert.Equal("*/10 * * * *", options.Get("SecondJob").Cron);
    }
}
