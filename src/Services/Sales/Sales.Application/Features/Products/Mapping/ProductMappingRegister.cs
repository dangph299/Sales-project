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
            .MapWith(source => new ProductDto(
                source.Id,
                source.Sku,
                source.Name,
                null,
                null,
                source.IsActive,
                source.Version,
                source.UpdatedAt,
                source.IsDelete,
                source.DeleteByUser,
                source.DeletedAt)
            {
                ProductCode = source.ProductCode,
                Description = source.Description,
                CategoryId = source.CategoryId,
                Status = source.Status.ToString(),
                Category = new ProductCategoryDto(Guid.Empty, string.Empty, string.Empty),
                Variants = Array.Empty<ProductVariantDto>()
            });
    }
}
