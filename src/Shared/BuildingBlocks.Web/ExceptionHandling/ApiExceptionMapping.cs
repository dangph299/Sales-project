using BuildingBlocks.Web.Models;

namespace BuildingBlocks.Web.ExceptionHandling;

/// <summary>
/// Client-safe HTTP error mapping produced from an exception.
/// </summary>
/// <param name="StatusCode">HTTP status code to return.</param>
/// <param name="ErrorCode">Stable public error code.</param>
/// <param name="Message">Client-safe error description.</param>
/// <param name="Errors">Optional non-field error details.</param>
/// <param name="ValidationErrors">Optional field-level validation errors.</param>
public sealed record ApiExceptionMapping(
    int StatusCode,
    string ErrorCode,
    string Message,
    IReadOnlyCollection<ApiError>? Errors = null,
    IReadOnlyCollection<ValidationError>? ValidationErrors = null);
