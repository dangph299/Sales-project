using MediatR;
using Sales.Application.Common.Exceptions;
using Sales.Application.Common.Interfaces;
using Sales.Application.Features.Products.Interfaces;
using Sales.Domain;

namespace Sales.Application.Features.Products.Commands;

/// <summary>
/// Handles <see cref="DeleteProduct"/> by soft-deleting the product and invalidating its cache.
/// </summary>
public sealed class DeleteProductHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IProductCache productCache,
    IExecutionContext executionContext)
    : IRequestHandler<DeleteProduct>
{
    /// <inheritdoc/>
    public async Task Handle(DeleteProduct request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, cancellationToken) ??
            throw new NotFoundException(nameof(Product), request.Id);
        product.Delete(executionContext.Actor);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await productCache.RemoveAsync(product.Id, cancellationToken);
    }
}
