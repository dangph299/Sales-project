using MediatR;
using Sales.Application.Common.Exceptions;
using Sales.Application.Features.Products.DTOs;
using Sales.Application.Features.Products.Interfaces;

namespace Sales.Application.Features.Products.Queries;

/// <summary>
/// Handles <see cref="GetProduct"/> by delegating to the product read service.
/// </summary>
public sealed class GetProductHandler(IProductReadService readService) : IRequestHandler<GetProduct, ProductDto>
{
    /// <summary>
    /// Loads the requested product.
    /// </summary>
    /// <param name="request">Query identifying the product.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Product, mapped to a <see cref="ProductDto"/>.</returns>
    /// <exception cref="NotFoundException">Thrown when no product exists with the given identifier.</exception>
    public async Task<ProductDto> Handle(GetProduct request, CancellationToken ct)
    {
        return await readService.GetAsync(request.Id, ct) ??
            throw new NotFoundException("Product", request.Id);
    }
}
