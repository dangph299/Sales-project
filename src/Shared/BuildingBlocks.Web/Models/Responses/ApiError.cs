namespace BuildingBlocks.Web.Models;

/// <summary>
/// Describes a single API error that is not tied to a specific input field.
/// Use it for reusable, client-safe error details in API responses.
/// </summary>
/// <param name="Code">Stable machine-readable error code.</param>
/// <param name="Message">Client-safe error message.</param>
public sealed record ApiError(string Code, string Message);
