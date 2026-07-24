using Sales.Application.Common.Exceptions;
using Sales.Application.Common.Interfaces;
using Sales.Application.Features.Products.DTOs;
using Sales.Application.Features.Products.Interfaces;
using Sales.Domain;

namespace Sales.Application.Features.Products.Commands;

public sealed class DeleteProductVariantHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IProductCache productCache,
    IProductReadService productReadService,
    IExecutionContext executionContext) : ICommandHandler<DeleteProductVariantCommand, ProductDto>
{
    public async Task<ProductDto> Handle(DeleteProductVariantCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetWithVariantsAsync(request.ProductId, cancellationToken) ??
            throw new NotFoundException(nameof(Product), request.ProductId);

        product.DeleteVariant(request.VariantId, executionContext.Actor);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await productCache.RemoveAsync(product.Id, cancellationToken);

        return await productReadService.GetForWriteResultAsync(product.Id, cancellationToken) ??
            throw new NotFoundException(nameof(Product), product.Id);
    }
}
