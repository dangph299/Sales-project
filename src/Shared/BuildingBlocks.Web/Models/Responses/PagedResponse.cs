namespace BuildingBlocks.Web.Models;

/// <summary>
/// Response model for a single page of API results.
/// Use it at HTTP boundaries when returning a subset of a larger result set.
/// </summary>
/// <typeparam name="T">Type of each item in the page.</typeparam>
/// <param name="Items">Items returned for the current page.</param>
/// <param name="TotalCount">Total number of items across all pages.</param>
/// <param name="PageNumber">1-based current page number.</param>
/// <param name="PageSize">Maximum number of items requested per page.</param>
public sealed record PagedResponse<T>(
    IReadOnlyCollection<T> Items,
    long TotalCount,
    int PageNumber,
    int PageSize)
{
    /// <summary>
    /// Gets the total number of available pages.
    /// </summary>
    public int TotalPages
    {
        get
        {
            if (PageSize <= 0)
            {
                return 0;
            }

            return (int)Math.Ceiling(TotalCount / (double)PageSize);
        }
    }

    /// <summary>
    /// Gets whether a previous page exists.
    /// </summary>
    public bool HasPreviousPage
    {
        get
        {
            return PageNumber > 1;
        }
    }

    /// <summary>
    /// Gets whether a next page exists.
    /// </summary>
    public bool HasNextPage
    {
        get
        {
            return PageNumber < TotalPages;
        }
    }
}
