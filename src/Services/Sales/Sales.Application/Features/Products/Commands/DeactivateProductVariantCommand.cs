using Sales.Application.Features.Products.DTOs;

namespace Sales.Application.Features.Products.Commands;

public sealed record DeactivateProductVariantCommand(Guid ProductId, Guid VariantId) : ICommand<ProductDto>;
