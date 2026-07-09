using MediatR;

namespace Sales.Application;

/// <summary>
/// Handles <see cref="GetProduct"/> by delegating to the product read service.
/// </summary>
public sealed class GetProductHandler(IProductReadService readService) : IRequestHandler<GetProduct, ProductDto>
{
    /// <summary>
    /// Loads the requested product.
    /// </summary>
    /// <param name="request">
    /// The query identifying the product to load.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The product, mapped to a <see cref="ProductDto"/>.
    /// </returns>
    /// <exception cref="NotFoundException">
    /// Thrown when no product exists with the given identifier.
    /// </exception>
    public async Task<ProductDto> Handle(GetProduct request, CancellationToken ct)
    {
        return await readService.GetAsync(request.Id, ct) ??
            throw new NotFoundException("Product", request.Id);
    }
}
