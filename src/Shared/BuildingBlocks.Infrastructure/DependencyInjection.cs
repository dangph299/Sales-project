using BuildingBlocks.Contracts;
using KafkaFlow.Producers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Registers shared infrastructure services that are independent of any bounded context.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds common infrastructure services used by service infrastructure projects.
    /// </summary>
    public static IServiceCollection AddBuildingBlocksInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddSharedLogging()
            .AddSharedOutbox();
    }

    /// <summary>
    /// Adds the shared Kafka outbox transport publisher for a service-specific producer name.
    /// </summary>
    public static IServiceCollection AddKafkaOutboxPublisher(
        this IServiceCollection services,
        string producerName)
    {
        services.TryAddSingleton<IOutboxPublisher>(sp => new KafkaOutboxPublisher(
            sp.GetRequiredService<IProducerAccessor>(),
            sp.GetRequiredService<ILogger<KafkaOutboxPublisher>>(),
            sp.GetRequiredService<System.Diagnostics.ActivitySource>(),
            producerName));
        return services;
    }

    private static IServiceCollection AddSharedLogging(this IServiceCollection services)
    {
        services.TryAddSingleton<IMessageLogContext, SerilogMessageLogContext>();
        return services;
    }

    private static IServiceCollection AddSharedOutbox(this IServiceCollection services)
    {
        services.TryAddSingleton<IOutboxSignal, OutboxSignal>();
        services.TryAddSingleton<IPersistenceExceptionClassifier, PostgresPersistenceExceptionClassifier>();
        return services;
    }
}
