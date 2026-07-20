namespace Sales.Application.Features.Products.DTOs;

public sealed record CreateProductVariantInput(Guid ColorId, Guid SizeId, decimal Price, string Status = "Draft");
