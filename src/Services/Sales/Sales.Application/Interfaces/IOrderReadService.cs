namespace Sales.Application;

/// <summary>
/// Query-side read port for orders, implemented directly against the database without going
/// through the command-side repository/aggregate.
/// </summary>
public interface IOrderReadService
{
    /// <summary>
    /// Gets a single order by its identifier.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the order to look up.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The order, or <see langword="null"/> if none exists.
    /// </returns>
    Task<OrderDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches orders by creation date range and/or customer name/phone.
    /// </summary>
    /// <param name="from">
    /// An optional inclusive lower bound on <c>CreatedAt</c>.
    /// </param>
    /// <param name="to">
    /// An optional inclusive upper bound on <c>CreatedAt</c>.
    /// </param>
    /// <param name="customer">
    /// An optional substring to match against the order's customer name or phone.
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
    /// A page of matching orders.
    /// </returns>
    Task<PagedResult<OrderDto>> SearchAsync(DateTimeOffset? from, DateTimeOffset? to, string? customer, int page, int pageSize, CancellationToken cancellationToken = default);
}
