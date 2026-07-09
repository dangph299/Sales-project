namespace Sales.Application;

/// <summary>
/// A single page of a larger result set.
/// </summary>
/// <typeparam name="T">
/// The type of item in the page.
/// </typeparam>
/// <param name="Items">
/// The items in this page.
/// </param>
/// <param name="Page">
/// The 1-based page number this page corresponds to.
/// </param>
/// <param name="PageSize">
/// The maximum number of items per page.
/// </param>
/// <param name="Total">
/// The total number of items across all pages.
/// </param>
public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, long Total);
