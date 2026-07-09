using AuditLog.Infrastructure;
using BuildingBlocks.Contracts;
using KafkaFlow;
using KafkaFlow.Serializer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuditLog.Worker;

/// <summary>
/// Composition-root extensions for registering the AuditLog Worker's services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers AuditLog Infrastructure, the Kafka consumer for every audit/integration topic,
    /// and the hosted services that start the audit store and Kafka bus.
    /// </summary>
    /// <param name="services">
    /// The service collection to register into.
    /// </param>
    /// <param name="configuration">
    /// The application configuration, used for Kafka broker/consumer group settings.
    /// </param>
    /// <returns>
    /// The same service collection, to allow chaining.
    /// </returns>
    public static IServiceCollection AddAuditLogWorker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuditLogInfrastructure(configuration);

        var brokers = configuration.GetSection("Kafka:Brokers").Get<string[]>() ?? ["kafka:9092"];
        var auditGroupId = configuration["Kafka:AuditGroupId"] ?? KafkaConsumerGroups.AuditMongoDb;
        services.AddKafka(kafka => kafka.UseMicrosoftLog().AddCluster(cluster => cluster.WithBrokers(brokers)
            .AddConsumer(c => c.Topics([
                    KafkaTopics.SalesAudit,
                    KafkaTopics.InventoryAudit,
                    KafkaTopics.OrderConfirmationRequested,
                    KafkaTopics.OrderUndoConfirmationRequested,
                    KafkaTopics.StockReserved,
                    KafkaTopics.StockRejected,
                    KafkaTopics.StockReleased])
                .WithGroupId(auditGroupId)
                .WithAutoOffsetReset(AutoOffsetReset.Earliest)
                .WithBufferSize(200)
                .WithWorkersCount(4)
                .AddMiddlewares(x => x.AddDeserializer<JsonCoreDeserializer>().AddTypedHandlers(h => h.AddHandler<AuditEventHandler>())))));
        services.AddHostedService<MongoStartupService>();
        services.AddHostedService<KafkaBusService>();
        return services;
    }
}
