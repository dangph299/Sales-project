using Sales.Application.Features.Customers.DTOs;

namespace Sales.Application.Features.Customers.Interfaces;

/// <summary>
/// Query-side read port for customers, implemented directly against the database without going
/// through the command-side repository/aggregate.
/// </summary>
public interface ICustomerReadService
{
    /// <summary>
    /// Gets a single customer by its identifier.
    /// </summary>
    /// <param name="id">Customer identifier.</param>
    /// <returns>Customer, or <see langword="null"/> if none exists.</returns>
    Task<CustomerDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches customers by name and/or phone number.
    /// </summary>
    /// <param name="name">An optional substring to match against the customer's name.</param>
    /// <param name="phone">An optional value matched against the start or the end of the customer's phone number.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum page size.</param>
    /// <returns>A page of matching customers.</returns>
    Task<PagedResult<CustomerDto>> SearchAsync(string? name, string? phone, int page, int pageSize, CancellationToken cancellationToken = default);
}
