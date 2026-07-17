using BuildingBlocks.Application.Mapping;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Sales.Infrastructure.Tests;

/// <summary>
/// Builds an <see cref="IMapper"/> through the same registration the composition root uses, so read
/// service tests map with the real scanned configuration.
/// </summary>
internal static class SalesMapperFactory
{
    internal static IMapper Create()
    {
        var services = new ServiceCollection();
        services.AddApplicationMapping(typeof(Sales.Application.DependencyInjection).Assembly);
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }
}
