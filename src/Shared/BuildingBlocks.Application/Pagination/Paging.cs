namespace BuildingBlocks.Application;

/// <summary>
/// Helpers for normalizing paging parameters supplied by callers.
/// </summary>
public static class Paging
{
    /// <summary>
    /// Clamps a requested page number and page size to safe bounds.
    /// </summary>
    /// <param name="page">Requested 1-based page number.</param>
    /// <param name="pageSize">Requested number of items per page.</param>
    /// <param name="maxPageSize">Maximum allowed page size.</param>
    /// <returns>Page number clamped to at least 1, and the page size clamped between 1 and <paramref name="maxPageSize"/>.</returns>
    public static (int Page, int PageSize) Normalize(int page, int pageSize, int maxPageSize = 100) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, maxPageSize));
}
