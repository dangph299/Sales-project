using MediatR;
using Sales.Application.Common.Exceptions;
using Sales.Application.Features.Products.DTOs;
using Sales.Application.Features.Products.Interfaces;
using Sales.Domain;

namespace Sales.Application.Features.Products.Commands;

/// <summary>
/// Handles <see cref="CreateProductCommand"/> by creating a product and its initial variants.
/// </summary>
public sealed class CreateProductHandler(
    IProductRepository productRepository,
    IRepository<Category> categoryRepository,
    IUnitOfWork unitOfWork,
    IProductCodeGenerator productCodeGenerator,
    IProductReadService productReadService) : IRequestHandler<CreateProductCommand, ProductDto>
{
    /// <inheritdoc/>
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken) ??
            throw new NotFoundException(nameof(Category), request.CategoryId);
        if (category.Status != ECategoryStatus.Published)
        {
            throw new DomainException("Only published categories can be assigned to products.");
        }

        // Allocated after the category check so a rejected request does not consume a number.
        var productCode = await productCodeGenerator.NextCodeAsync(cancellationToken);
        var product = Product.Create(productCode, request.Name, request.Description, request.CategoryId);
        foreach (var productVariantInput in request.Variants ?? [])
        {
            var productVariantStatus = ParseVariantStatus(productVariantInput.Status);
            var color = await productRepository.GetColorAsync(productVariantInput.ColorId, cancellationToken) ??
                throw new NotFoundException(nameof(Color), productVariantInput.ColorId);
            var size = await productRepository.GetSizeAsync(productVariantInput.SizeId, cancellationToken) ??
                throw new NotFoundException(nameof(Size), productVariantInput.SizeId);

            if (productVariantStatus == EProductVariantStatus.Published)
            {
                product.Publish();
            }

            product.AddVariant(color, size, productVariantInput.Price, productVariantStatus);
        }

        await productRepository.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await productReadService.GetForWriteResultAsync(product.Id, cancellationToken) ??
            throw new NotFoundException(nameof(Product), product.Id);
    }

    private static EProductVariantStatus ParseVariantStatus(string status)
    {
        if (Enum.TryParse<EProductVariantStatus>(status, ignoreCase: true, out var productVariantStatus))
        {
            return productVariantStatus;
        }

        throw new DomainException("Product variant status is invalid.");
    }
}
