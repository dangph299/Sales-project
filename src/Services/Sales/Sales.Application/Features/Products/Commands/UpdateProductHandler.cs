using Sales.Application.Common.Exceptions;
using Sales.Application.Features.Products.DTOs;
using Sales.Application.Features.Products.Interfaces;
using Sales.Domain;

namespace Sales.Application.Features.Products.Commands;

/// <summary>
/// Handles <see cref="UpdateProductCommand"/> by updating product common details.
/// </summary>
public sealed class UpdateProductHandler(
    IProductRepository productRepository,
    IRepository<Category> categoryRepository,
    IUnitOfWork unitOfWork,
    IProductCache productCache,
    IProductReadService productReadService) : ICommandHandler<UpdateProductCommand, ProductDto>
{
    /// <inheritdoc/>
    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetWithVariantsAsync(request.Id, cancellationToken) ??
            throw new NotFoundException(nameof(Product), request.Id);
        var category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken) ??
            throw new NotFoundException(nameof(Category), request.CategoryId);
        if (category.Status != ECategoryStatus.Published)
        {
            throw new DomainException("Only published categories can be assigned to products.");
        }

        product.Update(request.Name, request.Description, request.CategoryId);
        ApplyStatus(product, ParseProductStatus(request.Status));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await productCache.RemoveAsync(product.Id, cancellationToken);

        return await productReadService.GetForWriteResultAsync(product.Id, cancellationToken) ??
            throw new NotFoundException(nameof(Product), product.Id);
    }

    private static EProductStatus ParseProductStatus(string status)
    {
        if (Enum.TryParse<EProductStatus>(status, ignoreCase: true, out var productStatus))
        {
            return productStatus;
        }

        throw new DomainException("Product status is invalid.");
    }

    private static void ApplyStatus(Product product, EProductStatus productStatus)
    {
        if (product.Status == productStatus) return;
        if (productStatus == EProductStatus.Published)
        {
            product.Publish();
            return;
        }

        if (productStatus == EProductStatus.Discontinued)
        {
            product.Discontinue();
            return;
        }

        throw new DomainException("Product status transition is invalid.");
    }
}
