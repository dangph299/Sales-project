using BuildingBlocks.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Web.Tests;

public sealed class ObservabilityRegistrationTests
{
    [Fact]
    public void AddBuildingBlocksObservability_registers_the_opentelemetry_pipeline()
    {
        var services = new ServiceCollection();

        services.AddBuildingBlocksObservability("test-service", tracingSourceNames: ["Test.Kafka"]);

        Assert.Contains(services, descriptor =>
            (descriptor.ServiceType.FullName ?? string.Empty).Contains("OpenTelemetry") ||
            (descriptor.ImplementationType?.FullName ?? string.Empty).Contains("OpenTelemetry"));
    }

    [Fact]
    public void AddBuildingBlocksObservability_requires_a_service_name()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddBuildingBlocksObservability(" "));
    }

    [Fact]
    public void AddBuildingBlocksLogging_registers_serilog_on_the_host()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.AddBuildingBlocksLogging("test-service");

        Assert.Contains(builder.Services, descriptor =>
            (descriptor.ServiceType.FullName ?? string.Empty).Contains("Serilog") ||
            (descriptor.ImplementationType?.FullName ?? string.Empty).Contains("Serilog"));
    }

    [Fact]
    public void AddBuildingBlocksLogging_requires_a_service_name()
    {
        var builder = Host.CreateApplicationBuilder();

        Assert.Throws<ArgumentException>(() => builder.AddBuildingBlocksLogging(string.Empty));
    }
}
