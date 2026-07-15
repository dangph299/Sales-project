using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Sales.Application;

/// <summary>
/// Composition-root extensions for registering the Sales Application layer's services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Sales MediatR handlers, FluentValidation validators, and the shared MediatR
    /// pipeline behaviors (error logging, logging, then validation, in that wrapping order) used by
    /// every Sales command/query.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddSalesApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateProduct>());
        services.AddValidatorsFromAssemblyContaining<CreateCustomerValidator>();
        services.AddSingleton<IApplicationExceptionClassifier, SalesApplicationExceptionClassifier>();
        services.AddApplicationBuildingBlocks();
        return services;
    }
}
