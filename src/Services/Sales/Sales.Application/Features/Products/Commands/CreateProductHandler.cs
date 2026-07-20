using MapsterMapper;
using MediatR;
using Sales.Application.Features.Products.DTOs;
using Sales.Application.Features.Products.Interfaces;
using Sales.Domain;

namespace Sales.Application.Features.Products.Commands;

/// <summary>
/// Handles <see cref="CreateProduct"/> by checking SKU uniqueness, creating and persisting a new
/// <see cref="Product"/> aggregate, and warming its cache entry.
/// </summary>
public sealed class CreateProductHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IProductCache productCache,
    IMapper mapper) : IRequestHandler<CreateProduct, ProductDto>
{
    /// <summary>
    /// Checks that the SKU is not already in use, creates the product, commits the unit of work,
    /// and warms the product cache.
    /// </summary>
    /// <param name="request">Command describing the product to create.</param>
    /// <returns>Created product, mapped to a <see cref="ProductDto"/>.</returns>
    /// <exception cref="Sales.Domain.DomainException">Thrown when the SKU is already in use, or the provided SKU/name/price is invalid.</exception>
    public async Task<ProductDto> Handle(CreateProduct request, CancellationToken cancellationToken)
    {
        if (await productRepository.GetBySkuAsync(request.Sku.Trim().ToUpperInvariant(), cancellationToken) is not null)
            throw new DomainException("SKU already exists.");
        var product = Product.Create(request.Sku, request.Name, request.Price);
        await productRepository.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var dto = mapper.Map<ProductDto>(product);
        await productCache.SetAsync(dto, cancellationToken);
        return dto;
    }
}
