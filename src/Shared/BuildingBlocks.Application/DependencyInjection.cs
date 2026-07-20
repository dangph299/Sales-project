using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Application;

/// <summary>
/// Shared Application-layer service registration.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers shared MediatR pipeline behaviors in the expected wrapping order.
    /// </summary>
    public static IServiceCollection AddApplicationBuildingBlocks(this IServiceCollection services)
    {
        services.TryAddSingleton<IClock, SystemClock>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }
}
