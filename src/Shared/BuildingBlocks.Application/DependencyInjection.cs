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
    /// <param name="services">
    /// The service collection to register into.
    /// </param>
    /// <returns>
    /// The same service collection, to allow chaining.
    /// </returns>
    public static IServiceCollection AddApplicationBuildingBlocks(this IServiceCollection services)
    {
        services.TryAddSingleton<IApplicationExceptionClassifier, DefaultApplicationExceptionClassifier>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ErrorLoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }
}
