namespace BuildingBlocks.Web.Models;

/// <summary>
/// Sort direction for API query models.
/// Use it with reusable sorting request types at HTTP boundaries.
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// Sort values from low to high or A to Z.
    /// </summary>
    Ascending,

    /// <summary>
    /// Sort values from high to low or Z to A.
    /// </summary>
    Descending
}
