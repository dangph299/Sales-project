namespace Sales.Application;

/// <summary>
/// Query-side read port for customers, implemented directly against the database without going
/// through the command-side repository/aggregate.
/// </summary>
public interface ICustomerReadService
{
    /// <summary>
    /// Gets a single customer by its identifier.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the customer to look up.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The customer, or <see langword="null"/> if none exists.
    /// </returns>
    Task<CustomerDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches customers by name and/or phone number.
    /// </summary>
    /// <param name="name">
    /// An optional substring to match against the customer's name.
    /// </param>
    /// <param name="phone">
    /// An optional value to match against the customer's phone number.
    /// </param>
    /// <param name="phoneMatch">
    /// How <paramref name="phone"/> should be matched (prefix or suffix).
    /// </param>
    /// <param name="page">
    /// The 1-based page number to return.
    /// </param>
    /// <param name="pageSize">
    /// The maximum number of items per page.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// A page of matching customers.
    /// </returns>
    Task<PagedResult<CustomerDto>> SearchAsync(string? name, string? phone, PhoneMatch phoneMatch, int page, int pageSize, CancellationToken cancellationToken = default);
}
