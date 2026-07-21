namespace Sales.Application.Features.Customers.Interfaces;

/// <summary>
/// Allocates customer codes. Codes are assigned by the backend on create and are never supplied by
/// a client.
/// </summary>
public interface ICustomerCodeGenerator
{
    /// <summary>
    /// Allocates the next customer code.
    /// </summary>
    Task<string> NextCodeAsync(CancellationToken cancellationToken);
}
