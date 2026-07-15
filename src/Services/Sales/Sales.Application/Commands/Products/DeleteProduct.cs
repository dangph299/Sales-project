namespace Sales.Application;

/// <summary>
/// Command to soft-delete an existing product.
/// </summary>
/// <param name="Id">Product identifier.</param>
public sealed record DeleteProduct(Guid Id) : ICommand;
