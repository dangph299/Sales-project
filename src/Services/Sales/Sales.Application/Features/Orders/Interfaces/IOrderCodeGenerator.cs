namespace Sales.Application.Features.Orders.Interfaces;

/// <summary>
/// Allocates order codes. Codes are assigned by the backend on create and are never supplied by
/// a client.
/// </summary>
public interface IOrderCodeGenerator
{
    /// <summary>
    /// Allocates the next order code.
    /// </summary>
    Task<string> NextCodeAsync(CancellationToken cancellationToken);
}
