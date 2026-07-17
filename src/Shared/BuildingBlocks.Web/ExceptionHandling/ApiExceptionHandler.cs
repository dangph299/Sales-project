using System.Diagnostics;
using BuildingBlocks.Contracts;
using BuildingBlocks.Web.Extensions;
using BuildingBlocks.Web.Models;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Web.ExceptionHandling;

/// <summary>
/// Converts unhandled API exceptions into the shared API error response schema, and is the single
/// place the HTTP boundary logs a failed request. It runs outside <c>RequestObservabilityMiddleware</c>'s
/// <c>LogContext</c> scope (the exception has already unwound past it by the time
/// <c>UseExceptionHandler</c> invokes this), so it reads correlation values straight off the
/// <see cref="HttpContext"/> rather than relying on ambient log properties.
/// </summary>
public sealed class ApiExceptionHandler(
    IErrorCatalog errorCatalog,
    IPersistenceExceptionClassifier persistenceExceptionClassifier,
    IOptions<ApiExceptionHandlingOptions> options,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    private const int ClientClosedRequestStatusCode = 499;

    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var mapping = MapException(exception);
        httpContext.Response.StatusCode = mapping.StatusCode;

        var traceId = httpContext.GetTraceId();
        var correlationId = httpContext.GetCorrelationId();

        LogFailure(httpContext, exception, mapping, traceId, correlationId);
        RecordOnActivity(exception, mapping);

        var response = new ApiErrorResponse(
            mapping.StatusCode,
            mapping.ErrorCode,
            mapping.Message,
            traceId,
            correlationId,
            mapping.Errors,
            mapping.ValidationErrors);

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }

    private void LogFailure(
        HttpContext httpContext,
        Exception exception,
        ApiExceptionMapping mapping,
        string traceId,
        string correlationId)
    {
        logger.Log(
            mapping.LogLevel,
            exception,
            "Request failed {ErrorCode} {StatusCode} {RequestMethod} {RequestPath} {TraceId} {CorrelationId}",
            mapping.ErrorCode,
            mapping.StatusCode,
            httpContext.Request.Method,
            httpContext.Request.Path.Value,
            traceId,
            correlationId);
    }

    private static void RecordOnActivity(Exception exception, ApiExceptionMapping mapping)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        // UseExceptionHandler stops the exception here, so ASP.NET Core instrumentation never sees an
        // unhandled exception and would otherwise record none. Attach it ourselves.
        activity.AddException(exception);
        activity.SetTag("error.code", mapping.ErrorCode);

        // Only 5xx is a server fault. Marking 4xx as Error would make every validation failure count
        // against the service's trace error rate.
        if (mapping.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            activity.SetStatus(ActivityStatusCode.Error, mapping.ErrorCode);
        }
    }

    private ApiExceptionMapping MapException(Exception exception)
    {
        var customMapping = options.Value.TryMap(exception, errorCatalog);
        if (customMapping is not null)
        {
            return customMapping;
        }

        // Losing a race for a row is normal contention, not a fault: Warning, not Error.
        var persistenceError = persistenceExceptionClassifier.Classify(exception);
        if (persistenceError is not null)
        {
            var error = errorCatalog.Get(persistenceError.Code);
            var errors = new[] { new ApiError("retryable", persistenceError.Retryable.ToString()) };
            return new ApiExceptionMapping(409, error.Code, error.Description, errors, LogLevel: LogLevel.Warning);
        }

        if (exception is ValidationException validationException)
        {
            return CreateMapping(400, ErrorCodes.Validation, LogLevel.Information, validationException.ToValidationErrors());
        }

        if (exception is UnauthorizedAccessException)
        {
            // A rejected credential is security-relevant even though the caller caused it.
            return CreateMapping(401, ErrorCodes.Unauthorized, LogLevel.Warning);
        }

        if (exception is BadHttpRequestException badRequestException)
        {
            return CreateMapping(badRequestException.StatusCode, ErrorCodes.InvalidRequest, LogLevel.Information);
        }

        if (exception is OperationCanceledException)
        {
            // The client hung up. Nothing here is actionable for us.
            return CreateMapping(ClientClosedRequestStatusCode, ErrorCodes.OperationCancelled, LogLevel.Information);
        }

        if (exception is KeyNotFoundException)
        {
            return CreateMapping(404, ErrorCodes.NotFound, LogLevel.Information);
        }

        return CreateMapping(500, ErrorCodes.InternalServerError, LogLevel.Error);
    }

    private ApiExceptionMapping CreateMapping(
        int statusCode,
        string errorCode,
        LogLevel logLevel,
        IReadOnlyCollection<ValidationError>? validationErrors = null)
    {
        var error = errorCatalog.Get(errorCode);
        return new ApiExceptionMapping(statusCode, error.Code, error.Description, null, validationErrors, logLevel);
    }
}
