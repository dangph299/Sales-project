using Sales.Application.Features.Products.DTOs;

namespace Sales.Application.Features.Products.Commands;

public sealed record DeleteProductVariantCommand(Guid ProductId, Guid VariantId) : ICommand<ProductDto>;
