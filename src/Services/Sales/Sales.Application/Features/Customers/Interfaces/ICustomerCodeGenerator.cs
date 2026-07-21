namespace Sales.Application.Features.Customers.Interfaces;

/// <summary>
/// Generates customer codes for new customers.
/// </summary>
public interface ICustomerCodeGenerator
{
    /// <summary>
    /// Returns the next available customer code.
    /// </summary>
    Task<string> NextCodeAsync(CancellationToken cancellationToken = default);
}
