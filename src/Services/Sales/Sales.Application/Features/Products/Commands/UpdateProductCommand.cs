using Sales.Application.Features.Products.DTOs;

namespace Sales.Application.Features.Products.Commands;

/// <summary>
/// Command to update an existing product's common details.
/// </summary>
public sealed record UpdateProductCommand(Guid Id, string Name, string? Description, Guid CategoryId, string Status) : ICommand<ProductDto>;
