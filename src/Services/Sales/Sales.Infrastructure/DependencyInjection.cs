using System.Diagnostics;
using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using BuildingBlocks.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sales.Application;
using KafkaFlow;
using KafkaFlow.Producers;
using KafkaFlow.Serializer;
using StackExchange.Redis;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Composition-root extensions for registering the Sales Infrastructure layer's services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Sales persistence, read services, execution context, caching, and messaging.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration, used for connection strings and Kafka broker settings.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddSalesInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SalesDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("Sales")));
        services.AddSalesRepositories();
        services.AddSalesReadServices();
        services.AddSalesExecutionContext();
        services.AddSalesCaching(configuration);
        services.AddSalesMessaging(configuration);
        services.AddScoped<MaintenanceJobs>();
        return services;
    }

    private static IServiceCollection AddSalesRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    private static IServiceCollection AddSalesReadServices(this IServiceCollection services)
    {
        services.AddScoped<ProductReadService>();
        services.AddScoped<IProductReadService, CachedProductReadService>();
        services.AddScoped<ICustomerReadService, CustomerReadService>();
        services.AddScoped<IOrderReadService, OrderReadService>();
        return services;
    }

    private static IServiceCollection AddSalesExecutionContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IExecutionContext, HttpExecutionContext>();
        return services;
    }

    private static IServiceCollection AddSalesCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IProductCache, ProductCache>();
        services.AddStackExchangeRedisCache(options => options.Configuration = configuration.GetConnectionString("Redis"));
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!));
        return services;
    }

    private static IServiceCollection AddSalesMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(new ActivitySource(ObservabilityNames.SalesKafka));
        services.AddSingleton<IOutboxPublisher>(sp => new KafkaOutboxPublisher(
            sp.GetRequiredService<IProducerAccessor>(), sp.GetRequiredService<ILogger<KafkaOutboxPublisher>>(),
            sp.GetRequiredService<ActivitySource>(), "sales-outbox"));
        var brokers = configuration.GetSection("Kafka:Brokers").GetChildren().Select(x => x.Value!).Where(x => x is not null).ToArray();
        if (brokers.Length == 0) brokers = ["kafka:9092"];
        services.AddKafka(kafka => kafka
            .UseMicrosoftLog()
            .AddCluster(cluster => cluster
                .WithBrokers(brokers)
                .AddProducer("sales-outbox", producer => producer.AddMiddlewares(x => x.AddSerializer<JsonCoreSerializer>()))
                .AddConsumer(consumer => consumer
                    .Topics([KafkaTopics.StockReserved, KafkaTopics.StockRejected, KafkaTopics.StockReleased])
                    .WithGroupId(KafkaConsumerGroups.SalesInventoryResults)
                    .WithAutoOffsetReset(AutoOffsetReset.Earliest)
                    .WithBufferSize(100)
                    .WithWorkersCount(4)
                    .AddMiddlewares(x => x.AddDeserializer<JsonCoreDeserializer>().AddTypedHandlers(h => h.AddHandler<SalesIntegrationEventHandler>())))));
        services.AddHostedService<SalesOutboxPublisher>();
        return services;
    }
}
