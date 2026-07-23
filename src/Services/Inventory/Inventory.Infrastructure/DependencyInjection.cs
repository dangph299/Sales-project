using System.Diagnostics;
using BuildingBlocks.Application;
using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using Inventory.Application;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Features.InventoryItems.Interfaces;
using Inventory.Application.Features.Reservations.Interfaces;
using Inventory.Domain;
using KafkaFlow;
using KafkaFlow.Serializer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

/// <summary>
/// Composition-root extensions for registering the Inventory Infrastructure layer's services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Inventory persistence, application services, and messaging.
    /// </summary>
    /// <param name="configuration">Application configuration, used for connection strings and Kafka broker settings.</param>
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);
        services.AddAuditing(options =>
        {
            options.ServiceName = "Inventory";
            options.TopicName = KafkaTopics.InventoryAudit;
            options.IgnoreEntity<OutboxMessage>();
            options.IgnoreEntity<InboxMessage>();
            options.IgnoreEntity<InventoryItem>();
        });
        services.AddDbContext<InventoryDbContext>((sp, options) => options
            .UseNpgsql(configuration.GetConnectionString("Inventory"))
            .AddInterceptors(
                sp.GetRequiredService<AuditTimestampSaveChangesInterceptor>(),
                sp.GetRequiredService<AuditSaveChangesInterceptor>()));
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IInventoryItemReadService, InventoryReadService>();
        services.AddScoped<IReservationReadService, InventoryReadService>();
        services.AddScoped<IUnitOfWork, InventoryUnitOfWork>();
        services.AddScoped<IInventoryTransactionManager, InventoryTransactionManager>();
        services.AddScoped<IInventoryInbox, InventoryInbox>();
        services.AddScoped<IInventoryEventOutbox, InventoryEventOutbox>();
        services.AddScoped<InventoryMaintenanceService>();
        services.AddHostedService<InventoryMaintenanceWorker>();
        services.AddSingleton<IInventoryMetrics, InventoryMetricsAdapter>();
        services.AddScoped<IAuditAggregateResolver, InventoryAuditAggregateResolver>();
        services.AddScoped<IAuditEnricher, ReservationAuditEnricher>();
        services.AddScoped<IIntegrationEventProcessor, InventoryIntegrationEventProcessor>();
        services.AddScoped<IInboxFailureRecorder, InventoryInboxFailureRecorder>();
        services.AddSingleton(new ActivitySource(InventoryObservability.KafkaActivitySourceName));
        services.AddKafkaOutboxPublisher("inventory-outbox");
        services.AddHostedService<InventoryOutboxPublisher>();
        services.AddHostedService<InventoryInboxRedriveService>();
        var brokers = configuration.GetSection("Kafka:Brokers").GetChildren().Select(x => x.Value!).Where(x => x is not null).ToArray();
        if (brokers.Length == 0) brokers = ["kafka:9092"];
        services.AddKafka(kafka => kafka
            .UseMicrosoftLog()
            .AddCluster(cluster => cluster
                .WithBrokers(brokers)
                .AddProducer("inventory-outbox", producer => producer.AddMiddlewares(x => x.AddSerializer<JsonCoreSerializer>()))
                .AddConsumer(consumer => consumer
                    .Topics([
                        KafkaTopics.OrderConfirmationRequested,
                        KafkaTopics.OrderUndoConfirmationRequested
                    ])
                    .WithGroupId(KafkaConsumerGroups.InventoryOrders)
                    .WithAutoOffsetReset(AutoOffsetReset.Earliest)
                    .WithBufferSize(100)
                    .WithWorkersCount(4)
                    .AddMiddlewares(x => x.AddDeserializer<JsonCoreDeserializer>()
                    .AddTypedHandlers(h => h.AddHandler<InventoryEventHandler>())))));
        return services;
    }
}
