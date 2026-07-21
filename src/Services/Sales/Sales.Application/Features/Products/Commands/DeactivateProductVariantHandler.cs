using MediatR;
using Sales.Application.Common.Exceptions;
using Sales.Application.Features.Products.DTOs;
using Sales.Application.Features.Products.Interfaces;
using Sales.Domain;

namespace Sales.Application.Features.Products.Commands;

public sealed class DeactivateProductVariantHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IProductCache productCache,
    IProductReadService productReadService) : IRequestHandler<DeactivateProductVariantCommand, ProductDto>
{
    public async Task<ProductDto> Handle(DeactivateProductVariantCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetWithVariantsAsync(request.ProductId, cancellationToken) ??
            throw new NotFoundException(nameof(Product), request.ProductId);
        product.DeactivateVariant(request.VariantId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await productCache.RemoveAsync(product.Id, cancellationToken);

        return await productReadService.GetForWriteResultAsync(product.Id, cancellationToken) ??
            throw new NotFoundException(nameof(Product), product.Id);
    }
}
