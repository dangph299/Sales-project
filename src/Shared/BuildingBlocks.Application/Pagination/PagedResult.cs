namespace BuildingBlocks.Application;

/// <summary>
/// A single page of a larger result set.
/// </summary>
/// <param name="Items">Items in this page.</param>
/// <param name="Page">The 1-based page number this page corresponds to.</param>
/// <param name="PageSize">Maximum page size.</param>
/// <param name="Total">Total number of items across all pages.</param>
public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, long Total);
