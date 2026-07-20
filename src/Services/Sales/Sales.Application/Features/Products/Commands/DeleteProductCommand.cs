namespace Sales.Application.Features.Products.Commands;

/// <summary>
/// Command to soft-delete an existing product.
/// </summary>
/// <param name="Id">Product identifier.</param>
public sealed record DeleteProductCommand(Guid Id) : ICommand;
