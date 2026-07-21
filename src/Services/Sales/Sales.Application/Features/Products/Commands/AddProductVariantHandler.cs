using MediatR;
using Sales.Application.Common.Exceptions;
using Sales.Application.Features.Products.DTOs;
using Sales.Application.Features.Products.Interfaces;
using Sales.Domain;

namespace Sales.Application.Features.Products.Commands;

public sealed class AddProductVariantHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IProductCache productCache,
    IProductReadService productReadService) : IRequestHandler<AddProductVariantCommand, ProductDto>
{
    public async Task<ProductDto> Handle(AddProductVariantCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetWithVariantsAsync(request.ProductId, cancellationToken) ??
            throw new NotFoundException(nameof(Product), request.ProductId);
        var color = await productRepository.GetColorAsync(request.ColorId, cancellationToken) ??
            throw new NotFoundException(nameof(Color), request.ColorId);
        var size = await productRepository.GetSizeAsync(request.SizeId, cancellationToken) ??
            throw new NotFoundException(nameof(Size), request.SizeId);

        product.AddVariant(color, size, request.Price, ParseVariantStatus(request.Status));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await productCache.RemoveAsync(product.Id, cancellationToken);

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
