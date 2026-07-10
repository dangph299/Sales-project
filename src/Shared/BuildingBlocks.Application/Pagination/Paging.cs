namespace BuildingBlocks.Application;

/// <summary>
/// Helpers for normalizing paging parameters supplied by callers.
/// </summary>
public static class Paging
{
    /// <summary>
    /// Clamps a requested page number and page size to safe bounds.
    /// </summary>
    /// <param name="page">
    /// The requested 1-based page number.
    /// </param>
    /// <param name="pageSize">
    /// The requested number of items per page.
    /// </param>
    /// <param name="maxPageSize">
    /// The maximum allowed page size.
    /// </param>
    /// <returns>
    /// The page number clamped to at least 1, and the page size clamped between 1 and <paramref name="maxPageSize"/>.
    /// </returns>
    public static (int Page, int PageSize) Normalize(int page, int pageSize, int maxPageSize = 100) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, maxPageSize));
}
