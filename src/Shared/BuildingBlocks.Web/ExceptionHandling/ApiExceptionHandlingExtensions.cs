using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Web.ExceptionHandling;

/// <summary>
/// Registers shared API exception handling services.
/// </summary>
public static class ApiExceptionHandlingExtensions
{
    /// <summary>
    /// Adds the shared API exception handler.
    /// </summary>
    /// <param name="services">Service collection for an API host.</param>
    public static IServiceCollection AddApiExceptionHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddOptions<ApiExceptionHandlingOptions>();
        return services;
    }

    /// <summary>
    /// Adds the shared API exception handler with service-specific mappings.
    /// </summary>
    /// <param name="services">Service collection for an API host.</param>
    /// <param name="configure">Service-specific exception mapping configuration.</param>
    public static IServiceCollection AddApiExceptionHandling(
        this IServiceCollection services,
        Action<ApiExceptionHandlingOptions> configure)
    {
        services.AddApiExceptionHandling();
        services.Configure(configure);
        return services;
    }
}
