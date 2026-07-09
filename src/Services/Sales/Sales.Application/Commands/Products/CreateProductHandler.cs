using MediatR;
using Sales.Domain;

namespace Sales.Application;

/// <summary>
/// Handles <see cref="CreateProduct"/> by checking SKU uniqueness, creating and persisting a new
/// <see cref="Product"/> aggregate, and warming its cache entry.
/// </summary>
public sealed class CreateProductHandler(IProductRepository repository, IUnitOfWork unitOfWork, IProductCache cache) : IRequestHandler<CreateProduct, ProductDto>
{
    /// <summary>
    /// Checks that the SKU is not already in use, creates the product, commits the unit of work,
    /// and warms the product cache.
    /// </summary>
    /// <param name="request">
    /// The command describing the product to create.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The created product, mapped to a <see cref="ProductDto"/>.
    /// </returns>
    /// <exception cref="Sales.Domain.DomainException">
    /// Thrown when the SKU is already in use, or the provided SKU/name/price is invalid.
    /// </exception>
    public async Task<ProductDto> Handle(CreateProduct request, CancellationToken ct)
    {
        if (await repository.GetBySkuAsync(request.Sku.Trim().ToUpperInvariant(), ct) is not null)
            throw new DomainException("SKU already exists.");
        var product = Product.Create(request.Sku, request.Name, request.Price);
        await repository.AddAsync(product, ct);
        await unitOfWork.SaveChangesAsync(ct);
        var dto = product.ToDto();
        await cache.SetAsync(dto, ct);
        return dto;
    }
}
