using Sales.Application.Features.Products.DTOs;

namespace Sales.Application.Features.Products.Commands;

/// <summary>
/// Command to create a new catalog product. The product code is allocated by the backend, so it is
/// not part of the request.
/// </summary>
public sealed record CreateProductCommand(
    string Name,
    string? Description,
    Guid CategoryId,
    IReadOnlyCollection<CreateProductVariantInput>? Variants = null) : ICommand<ProductDto>;
