namespace BuildingBlocks.Web.Models;

/// <summary>
/// Base request model for paged API queries.
/// Use it for HTTP query models that accept page number and page size.
/// </summary>
public class PagedRequest
{
    private int _pageNumber = 1;
    private int _pageSize = 20;

    /// <summary>
    /// Gets the default maximum page size used when no endpoint-specific limit is supplied.
    /// </summary>
    public const int DefaultMaxPageSize = 100;

    /// <summary>
    /// Gets or initializes the 1-based page number, clamped to at least 1.
    /// </summary>
    public int PageNumber
    {
        get
        {
            return _pageNumber;
        }

        init
        {
            _pageNumber = Math.Max(1, value);
        }
    }

    /// <summary>
    /// Gets or initializes the page size, clamped between 1 and <see cref="DefaultMaxPageSize"/>.
    /// </summary>
    public int PageSize
    {
        get
        {
            return _pageSize;
        }

        init
        {
            _pageSize = Math.Clamp(value, 1, DefaultMaxPageSize);
        }
    }
}
