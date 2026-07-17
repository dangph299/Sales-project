using BuildingBlocks.Application.Mapping;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Sales.Application.Common.Exceptions;

namespace Sales.Application;

/// <summary>
/// Composition-root extensions for registering the Sales Application layer's services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Sales MediatR handlers, FluentValidation validators, Mapster mapping registers,
    /// and the shared MediatR pipeline behaviors (error logging, logging, then validation, in that
    /// wrapping order) used by every Sales command/query.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddSalesApplication(this IServiceCollection services)
    {
        var salesApplicationAssembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(salesApplicationAssembly));
        services.AddValidatorsFromAssembly(salesApplicationAssembly);
        services.AddApplicationMapping(salesApplicationAssembly);
        services.AddSingleton<IApplicationExceptionClassifier, SalesApplicationExceptionClassifier>();
        services.AddApplicationBuildingBlocks();
        return services;
    }
}
