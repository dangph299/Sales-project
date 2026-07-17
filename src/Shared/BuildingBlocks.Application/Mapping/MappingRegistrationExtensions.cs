using System.Reflection;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Application.Mapping;

/// <summary>
/// Shared Mapster registration for Application layers. Owns the mechanism only — each service keeps
/// its own <see cref="IRegister"/> implementations in its Application assembly.
/// </summary>
public static class MappingRegistrationExtensions
{
    /// <summary>
    /// Builds one <see cref="TypeAdapterConfig"/> from the <see cref="IRegister"/> implementations
    /// found in the given assemblies and registers it alongside an <see cref="IMapper"/> that uses it.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="assemblies">Application assemblies to scan for mapping registers.</param>
    /// <returns>Service collection for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when no assembly is provided.</exception>
    public static IServiceCollection AddApplicationMapping(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        if (assemblies.Length == 0)
        {
            throw new ArgumentException(
                "At least one application assembly must be provided.",
                nameof(assemblies));
        }

        var mappingConfig = new TypeAdapterConfig();
        mappingConfig.Scan(assemblies);

        services.AddSingleton(mappingConfig);
        services.AddScoped<IMapper, Mapper>();

        return services;
    }
}
