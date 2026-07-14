namespace BuildingBlocks.Web.Models;

/// <summary>
/// Base request model for sorted and paged API queries.
/// Use it for HTTP query models that accept paging and generic sorting metadata.
/// </summary>
public class SortRequest : PagedRequest
{
    /// <summary>
    /// Gets or initializes the optional field name to sort by.
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Gets or initializes the requested sort direction.
    /// </summary>
    public SortDirection SortDirection { get; init; } = SortDirection.Ascending;
}
