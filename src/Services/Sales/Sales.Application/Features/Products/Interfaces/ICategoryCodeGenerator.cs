namespace Sales.Application.Features.Products.Interfaces;

/// <summary>
/// Allocates category codes. Codes are assigned by the backend on create and are never supplied by
/// a client.
/// </summary>
public interface ICategoryCodeGenerator
{
    /// <summary>
    /// Allocates the next category code.
    /// </summary>
    Task<string> NextCodeAsync(CancellationToken cancellationToken);
}
