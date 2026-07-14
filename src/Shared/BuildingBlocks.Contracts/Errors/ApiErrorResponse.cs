namespace BuildingBlocks.Contracts;

/// <summary>
/// Common API error response contract.
/// </summary>
/// <param name="Code">Stable machine-readable error code.</param>
/// <param name="Description">Client-safe error description.</param>
/// <param name="Status">HTTP status code.</param>
/// <param name="TraceId">Request trace identifier.</param>
/// <param name="Errors">Validation errors keyed by field.</param>
public sealed record ApiErrorResponse(
    string Code,
    string Description,
    int Status,
    string? TraceId = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);
