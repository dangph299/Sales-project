using MediatR;
using Sales.Domain;

namespace Sales.Application;

/// <summary>
/// Handles <see cref="CreateProduct"/> by checking SKU uniqueness, creating and persisting a new
/// <see cref="Product"/> aggregate, and warming its cache entry.
/// </summary>
public sealed class CreateProductHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IProductCache productCache) : IRequestHandler<CreateProduct, ProductDto>
{
    /// <summary>
    /// Checks that the SKU is not already in use, creates the product, commits the unit of work,
    /// and warms the product cache.
    /// </summary>
    /// <param name="request">Command describing the product to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created product, mapped to a <see cref="ProductDto"/>.</returns>
    /// <exception cref="Sales.Domain.DomainException">Thrown when the SKU is already in use, or the provided SKU/name/price is invalid.</exception>
    public async Task<ProductDto> Handle(CreateProduct request, CancellationToken cancellationToken)
    {
        if (await productRepository.GetBySkuAsync(request.Sku.Trim().ToUpperInvariant(), cancellationToken) is not null)
            throw new DomainException("SKU already exists.");
        var product = Product.Create(request.Sku, request.Name, request.Price);
        await productRepository.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var dto = product.ToDto();
        await productCache.SetAsync(dto, cancellationToken);
        return dto;
    }
}
