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
/// Converts unhandled API exceptions into the shared API error response schema.
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

        var response = new ApiErrorResponse(
            mapping.StatusCode,
            mapping.ErrorCode,
            mapping.Message,
            httpContext.TraceIdentifier,
            httpContext.GetCorrelationId(),
            mapping.Errors,
            mapping.ValidationErrors);

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }

    private ApiExceptionMapping MapException(Exception exception)
    {
        var customMapping = options.Value.TryMap(exception, errorCatalog);
        if (customMapping is not null)
        {
            return customMapping;
        }

        var persistenceError = persistenceExceptionClassifier.Classify(exception);
        if (persistenceError is not null)
        {
            var error = errorCatalog.Get(persistenceError.Code);
            var errors = new[] { new ApiError("retryable", persistenceError.Retryable.ToString()) };
            return new ApiExceptionMapping(409, error.Code, error.Description, errors);
        }

        if (exception is ValidationException validationException)
        {
            return CreateMapping(400, ErrorCodes.Validation, validationException.ToValidationErrors());
        }

        if (IsDomainException(exception))
        {
            return CreateMapping(400, ErrorCodes.InvalidOperation);
        }

        if (exception is UnauthorizedAccessException)
        {
            return CreateMapping(401, ErrorCodes.Unauthorized);
        }

        if (exception.GetType().Name == "ForbiddenException")
        {
            return CreateMapping(403, ErrorCodes.Forbidden);
        }

        if (exception is BadHttpRequestException badRequestException)
        {
            return CreateMapping(badRequestException.StatusCode, ErrorCodes.InvalidRequest);
        }

        if (exception is OperationCanceledException)
        {
            return CreateMapping(ClientClosedRequestStatusCode, ErrorCodes.OperationCancelled);
        }

        if (exception is KeyNotFoundException)
        {
            return CreateMapping(404, ErrorCodes.NotFound);
        }

        logger.LogError(exception, "Unhandled API exception");
        return CreateMapping(500, ErrorCodes.InternalServerError);
    }

    private ApiExceptionMapping CreateMapping(
        int statusCode,
        string errorCode,
        IReadOnlyCollection<ValidationError>? validationErrors = null)
    {
        var error = errorCatalog.Get(errorCode);
        return new ApiExceptionMapping(statusCode, error.Code, error.Description, null, validationErrors);
    }

    private static bool IsDomainException(Exception exception)
    {
        var exceptionType = exception.GetType();
        return exceptionType.Name == "DomainException" || exceptionType.BaseType?.Name == "DomainException";
    }
}
