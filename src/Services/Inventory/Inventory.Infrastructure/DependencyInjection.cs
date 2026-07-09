using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using Inventory.Application;
using KafkaFlow;
using KafkaFlow.Producers;
using KafkaFlow.Serializer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure;

/// <summary>
/// Composition-root extensions for registering the Inventory Infrastructure layer's services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Inventory database context, application service, outbox publisher, and Kafka
    /// consumer for Sales order events.
    /// </summary>
    /// <param name="services">
    /// The service collection to register into.
    /// </param>
    /// <param name="configuration">
    /// The application configuration, used for connection strings and Kafka broker settings.
    /// </param>
    /// <returns>
    /// The same service collection, to allow chaining.
    /// </returns>
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<InventoryDbContext>(x => x.UseNpgsql(configuration.GetConnectionString("Inventory")));
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddSingleton<IOutboxPublisher>(sp => new KafkaOutboxPublisher(
            sp.GetRequiredService<IProducerAccessor>(), sp.GetRequiredService<ILogger<KafkaOutboxPublisher>>(),
            InventoryActivitySource.Instance, "inventory-outbox"));
        services.AddHostedService<InventoryOutboxPublisher>();
        var brokers = configuration.GetSection("Kafka:Brokers").GetChildren().Select(x => x.Value!).Where(x => x is not null).ToArray();
        if (brokers.Length == 0) brokers = ["kafka:9092"];
        services.AddKafka(kafka => kafka.UseMicrosoftLog().AddCluster(cluster => cluster.WithBrokers(brokers)
            .AddProducer("inventory-outbox", p => p.AddMiddlewares(x => x.AddSerializer<JsonCoreSerializer>()))
            .AddConsumer(c => c.Topics([KafkaTopics.OrderConfirmationRequested, KafkaTopics.OrderUndoConfirmationRequested])
                .WithGroupId(KafkaConsumerGroups.InventoryOrders).WithAutoOffsetReset(AutoOffsetReset.Earliest).WithBufferSize(100).WithWorkersCount(4)
                .AddMiddlewares(x => x.AddDeserializer<JsonCoreDeserializer>().AddTypedHandlers(h => h.AddHandler<InventoryEventHandler>())))));
        return services;
    }
}
