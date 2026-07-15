using BuildingBlocks.Contracts;
using BuildingBlocks.Web.ExceptionHandling;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Web.Tests;

public sealed class WebHostRegistrationTests
{
    private static IConfiguration EmptyConfiguration()
    {
        return new ConfigurationBuilder().Build();
    }

    private static void ConfigureValid(BuildingBlocksWebOptions options)
    {
        options.ServiceName = "test-api";
        options.ApiTitle = "Test API";
        options.ApiDescription = "Test API for unit tests.";
        options.ActivitySourceName = "Test.Kafka";
        options.MeterName = "Test.Metrics";
    }

    [Fact]
    public void AddBuildingBlocksWeb_registers_the_shared_error_catalog_once()
    {
        var services = new ServiceCollection();

        services.AddBuildingBlocksWeb(EmptyConfiguration(), ConfigureValid);

        var catalog = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IErrorCatalog));
        Assert.Equal(typeof(ErrorCatalogResolver), catalog.ImplementationType);
    }

    [Fact]
    public void AddBuildingBlocksWeb_registers_the_shared_exception_handler()
    {
        var services = new ServiceCollection();

        services.AddBuildingBlocksWeb(EmptyConfiguration(), ConfigureValid);

        Assert.Contains(services, descriptor => descriptor.ImplementationType == typeof(ApiExceptionHandler));
    }

    [Fact]
    public void AddBuildingBlocksWeb_registers_the_observability_pipeline()
    {
        var services = new ServiceCollection();

        services.AddBuildingBlocksWeb(EmptyConfiguration(), ConfigureValid);

        Assert.Contains(services, descriptor =>
            (descriptor.ServiceType.FullName ?? string.Empty).Contains("OpenTelemetry") ||
            (descriptor.ImplementationType?.FullName ?? string.Empty).Contains("OpenTelemetry"));
    }

    [Fact]
    public void AddBuildingBlocksWeb_throws_when_a_required_option_is_missing()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddBuildingBlocksWeb(EmptyConfiguration(), options => options.ApiTitle = "Only a title"));
    }
}
