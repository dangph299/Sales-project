using Sales.Application.Features.Products.DTOs;

namespace Sales.Application.Features.Products.Commands;

public sealed record UpdateProductVariantCommand(
    Guid ProductId,
    Guid VariantId,
    Guid ColorId,
    Guid SizeId,
    decimal Price,
    string Status) : ICommand<ProductDto>;
