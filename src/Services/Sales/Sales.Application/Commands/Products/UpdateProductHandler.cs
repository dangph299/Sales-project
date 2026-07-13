using MediatR;
using Sales.Domain;

namespace Sales.Application;

/// <summary>
/// Handles <see cref="UpdateProduct"/> by loading and updating an existing <see cref="Product"/>
/// aggregate, then invalidating its cache entry.
/// </summary>
public sealed class UpdateProductHandler(IProductRepository repository, IUnitOfWork unitOfWork, IProductCache cache) : IRequestHandler<UpdateProduct, ProductDto>
{
    /// <summary>
    /// Loads the product, applies the update, commits the unit of work, and invalidates the cached entry.
    /// </summary>
    /// <param name="request">Command describing the product to update and its new values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated product, mapped to a <see cref="ProductDto"/>.</returns>
    /// <exception cref="NotFoundException">Thrown when no product exists with the given identifier.</exception>
    /// <exception cref="Sales.Domain.DomainException">Thrown when the provided name or price is invalid.</exception>
    public async Task<ProductDto> Handle(UpdateProduct request, CancellationToken ct)
    {
        var product = await repository.GetByIdAsync(request.Id, ct) ?? throw new NotFoundException(nameof(Product), request.Id);
        product.Update(request.Name, request.Price, request.IsActive);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveAsync(product.Id, ct);
        return product.ToDto();
    }
}
