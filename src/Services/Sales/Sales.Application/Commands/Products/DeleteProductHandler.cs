using MediatR;
using Sales.Domain;

namespace Sales.Application;

/// <summary>
/// Handles <see cref="DeleteProduct"/> by soft-deleting the product and invalidating its cache.
/// </summary>
public sealed class DeleteProductHandler(IProductRepository repository, IUnitOfWork unitOfWork, IProductCache cache, IExecutionContext context)
    : IRequestHandler<DeleteProduct>
{
    /// <inheritdoc/>
    public async Task Handle(DeleteProduct request, CancellationToken ct)
    {
        var product = await repository.GetByIdAsync(request.Id, ct) ?? throw new NotFoundException(nameof(Product), request.Id);
        product.Delete(context.Actor);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveAsync(product.Id, ct);
    }
}
