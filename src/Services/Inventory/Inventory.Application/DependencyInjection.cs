using BuildingBlocks.Application.Mapping;
using FluentValidation;
using Inventory.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Application;

/// <summary>
/// Composition-root extensions for registering the Inventory Application layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Inventory validators, mapping, and MediatR pipeline behaviors (shared behaviors from
    /// <c>BuildingBlocks.Application</c> plus the Inventory-specific transaction/inbox behavior).
    /// Registered after the shared behaviors so <c>ValidationBehavior</c> still runs before
    /// <see cref="InventoryTransactionBehavior{TRequest,TResponse}"/> opens a transaction.
    /// </summary>
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services)
    {
        var inventoryApplicationAssembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(inventoryApplicationAssembly));
        services.AddValidatorsFromAssembly(inventoryApplicationAssembly);
        services.AddApplicationMapping(inventoryApplicationAssembly);
        services.AddApplicationBuildingBlocks();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(InventoryTransactionBehavior<,>));
        return services;
    }
}
