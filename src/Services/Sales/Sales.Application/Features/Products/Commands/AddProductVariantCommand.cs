using Sales.Application.Features.Products.DTOs;

namespace Sales.Application.Features.Products.Commands;

public sealed record AddProductVariantCommand(Guid ProductId, Guid ColorId, Guid SizeId, decimal Price, string Status = "Draft") : ICommand<ProductDto>;
