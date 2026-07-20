using System.Diagnostics;
using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using KafkaFlow;
using KafkaFlow.Serializer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sales.Application.Common.Interfaces;
using Sales.Application.Features.Customers.Interfaces;
using Sales.Application.Features.Orders.Interfaces;
using Sales.Application.Features.Products.Interfaces;
using Sales.Domain;
using StackExchange.Redis;

namespace Sales.Infrastructure;

/// <summary>
/// Composition-root extensions for registering the Sales Infrastructure layer's services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Sales persistence, read services, execution context, caching, and messaging.
    /// </summary>
    /// <param name="configuration">Application configuration, used for connection strings and Kafka broker settings.</param>
    public static IServiceCollection AddSalesInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);
        services.AddAuditing(options =>
        {
            options.ServiceName = "Sales";
            options.TopicName = KafkaTopics.SalesAudit;
            options.IgnoreEntity<OutboxMessage>();
            options.IgnoreEntity<InboxMessage>();
            options.IgnoreEntity<ApplicationUser>();
            options.IgnoreEntity<RefreshToken>();
        });
        services.AddDbContext<SalesDbContext>((sp, options) => options
            .UseNpgsql(configuration.GetConnectionString("Sales"))
            .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));
        services.AddSalesRepositories();
        services.AddSalesReadServices();
        services.AddSalesExecutionContext();
        services.AddSalesAuditing();
        services.AddSalesCaching(configuration);
        services.AddSalesMessaging(configuration);
        services.AddScoped<SalesMaintenanceService>();
        services.AddSalesHangfireJobs(configuration);
        return services;
    }

    private static IServiceCollection AddSalesHangfireJobs(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddSalesRecurringJobs(configuration);
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
        services.AddScoped<IProductReadService>(sp =>
        {
            var inner = sp.GetRequiredService<ProductReadService>();
            var cache = sp.GetRequiredService<IProductCache>();
            return new CachedProductReadService(inner, cache);
        });
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

    private static IServiceCollection AddSalesAuditing(this IServiceCollection services)
    {
        services.AddScoped<IAuditContextAccessor, SalesAuditContextAccessor>();
        services.AddScoped<IAuditAggregateResolver, SalesAuditAggregateResolver>();
        services.AddScoped<IAuditEnricher, OrderAuditEnricher>();
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
        services.AddSingleton(new ActivitySource(SalesObservability.KafkaActivitySourceName));
        services.AddScoped<IIntegrationEventProcessor, SalesInventoryEventProcessor>();
        services.AddScoped<IInboxFailureRecorder, SalesInboxFailureRecorder>();
        services.AddKafkaOutboxPublisher("sales-outbox");
        var brokers = configuration.GetSection("Kafka:Brokers").GetChildren().Select(x => x.Value!).Where(x => x is not null).ToArray();
        if (brokers.Length == 0) brokers = ["kafka:9092"];
        services.AddKafka(kafka => kafka
            .UseMicrosoftLog()
            .AddCluster(cluster => cluster
                .WithBrokers(brokers)
                .AddProducer("sales-outbox", producer => producer.AddMiddlewares(x => x.AddSerializer<JsonCoreSerializer>()))
                .AddConsumer(consumer => consumer
                    .Topics([
                        KafkaTopics.StockReserved, 
                        KafkaTopics.StockRejected, 
                        KafkaTopics.StockReleased
                    ])
                    .WithGroupId(KafkaConsumerGroups.SalesInventoryResults)
                    .WithAutoOffsetReset(AutoOffsetReset.Earliest)
                    .WithBufferSize(100)
                    .WithWorkersCount(4)
                    .AddMiddlewares(x => x.AddDeserializer<JsonCoreDeserializer>()
                    .AddTypedHandlers(h => h.AddHandler<SalesIntegrationEventHandler>())))));
        services.AddHostedService<SalesOutboxPublisher>();
        services.AddHostedService<SalesInboxRedriveService>();
        return services;
    }
}
