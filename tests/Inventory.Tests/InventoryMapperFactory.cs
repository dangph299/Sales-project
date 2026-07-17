using BuildingBlocks.Application.Mapping;
using Inventory.Application;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Tests;

/// <summary>
/// Builds an <see cref="IMapper"/> through the same registration the composition root uses, so
/// tests exercise the real scanned configuration rather than a hand-built one.
/// </summary>
internal static class InventoryMapperFactory
{
    internal static IMapper Create()
    {
        var services = new ServiceCollection();
        services.AddApplicationMapping(typeof(DependencyInjection).Assembly);
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }
}
