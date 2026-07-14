using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Application;

/// <summary>
/// Composition-root extensions for registering the Inventory Application layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Inventory validators and MediatR pipeline behaviors (shared behaviors from
    /// <c>BuildingBlocks.Application</c> plus the Inventory-specific transaction/inbox behavior).
    /// Registered after the shared behaviors so <c>ValidationBehavior</c> still runs before
    /// <see cref="InventoryTransactionBehavior{TRequest,TResponse}"/> opens a transaction.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<AdjustInventoryCommandValidator>();
        services.AddApplicationBuildingBlocks();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(InventoryTransactionBehavior<,>));
        return services;
    }
}
