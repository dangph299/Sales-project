using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Registers shared EF Core audit generation services.
/// </summary>
public static class AuditingServiceCollectionExtensions
{
    /// <summary>
    /// Adds shared audit services and options.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Optional audit configuration.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddAuditing(
        this IServiceCollection services,
        Action<AuditOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddScoped<IAuditContextAccessor, SystemAuditContextAccessor>();
        services.TryAddScoped<IAuditAggregateResolver, DefaultAuditAggregateResolver>();
        services.TryAddScoped<IAuditEntryFactory, EfCoreAuditEntryFactory>();
        services.TryAddScoped<AuditSaveChangesInterceptor>();
        return services;
    }
}
