using MediatR;

namespace Sales.Application;

/// <summary>
/// Handles <see cref="SearchOrders"/> by delegating to the order read service.
/// </summary>
public sealed class SearchOrdersHandler(IOrderReadService readService) : IRequestHandler<SearchOrders, PagedResult<OrderDto>>
{
    /// <summary>
    /// Searches orders matching the given criteria.
    /// </summary>
    /// <param name="request">
    /// The query describing the search criteria and paging.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// A page of matching orders.
    /// </returns>
    public async Task<PagedResult<OrderDto>> Handle(SearchOrders request, CancellationToken ct)
    {
        return await readService.SearchAsync(request.From, request.To, request.Customer, request.Page, request.PageSize, ct);
    }
}
