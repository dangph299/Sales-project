using Sales.Application.Features.Products.DTOs;

namespace Sales.Application.Features.Products.Commands;

/// <summary>
/// Command to create a new catalog product.
/// </summary>
public sealed record CreateProductCommand(
    string ProductCode,
    string Name,
    string? Description,
    Guid CategoryId,
    IReadOnlyCollection<CreateProductVariantInput>? Variants = null) : ICommand<ProductDto>;
