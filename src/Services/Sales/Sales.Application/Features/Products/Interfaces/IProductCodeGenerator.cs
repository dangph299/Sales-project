namespace Sales.Application.Features.Products.Interfaces;

/// <summary>
/// Allocates product codes. Codes are assigned by the backend on create and are never supplied by
/// a client.
/// </summary>
public interface IProductCodeGenerator
{
    /// <summary>
    /// Allocates the next product code.
    /// </summary>
    Task<string> NextCodeAsync(CancellationToken cancellationToken);
}
