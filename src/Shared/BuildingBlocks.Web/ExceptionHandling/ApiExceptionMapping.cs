using BuildingBlocks.Web.Models;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Web.ExceptionHandling;

/// <summary>
/// Client-safe HTTP error mapping produced from an exception. This is the only place that knows
/// both the public error code and the HTTP status for a failure, so it also carries the severity
/// the failure is logged at.
/// </summary>
/// <param name="StatusCode">HTTP status code to return.</param>
/// <param name="ErrorCode">Stable public error code.</param>
/// <param name="Message">Client-safe error description.</param>
/// <param name="Errors">Optional non-field error details.</param>
/// <param name="ValidationErrors">Optional field-level validation errors.</param>
/// <param name="LogLevel">
/// Severity this failure is logged at. Defaults to <see cref="LogLevel.Error"/> so a mapping that
/// forgets to classify itself is loud rather than silent. Use <see cref="LogLevel.Information"/>
/// for failures caused by client input (validation, not found, business rule rejections),
/// <see cref="LogLevel.Warning"/> for conflicts and retryable infrastructure contention, and
/// <see cref="LogLevel.Error"/> for failures that need an engineer.
/// </param>
public sealed record ApiExceptionMapping(
    int StatusCode,
    string ErrorCode,
    string Message,
    IReadOnlyCollection<ApiError>? Errors = null,
    IReadOnlyCollection<ValidationError>? ValidationErrors = null,
    LogLevel LogLevel = LogLevel.Error);
