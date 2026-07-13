using BuildingBlocks.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sales.Infrastructure.Tests;

public sealed class BuildingBlocksInfrastructureRegistrationTests
{
    [Fact]
    public void Shared_infrastructure_registration_resolves_shared_components()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddBuildingBlocksInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.IsType<OutboxSignal>(provider.GetRequiredService<IOutboxSignal>());
        Assert.IsType<SerilogMessageLogContext>(provider.GetRequiredService<IMessageLogContext>());
    }

    [Fact]
    public void Kafka_outbox_publisher_is_registered_by_shared_extension()
    {
        var services = new ServiceCollection();

        services.AddKafkaOutboxPublisher("test-outbox");

        var descriptor = Assert.Single(services, x => x.ServiceType == typeof(IOutboxPublisher));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.NotNull(descriptor.ImplementationFactory);
    }
}
