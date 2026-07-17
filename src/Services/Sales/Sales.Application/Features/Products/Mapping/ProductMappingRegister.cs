using Mapster;
using Sales.Application.Features.Products.DTOs;
using Sales.Domain;

namespace Sales.Application.Features.Products.Mapping;

/// <summary>
/// Mapping configuration owned by the <see cref="Product"/> aggregate root.
/// </summary>
public sealed class ProductMappingRegister : IRegister
{
    /// <inheritdoc/>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Product, ProductDto>()
            .Map(
                destination => destination.Price,
                source => source.Price.Amount);
    }
}
