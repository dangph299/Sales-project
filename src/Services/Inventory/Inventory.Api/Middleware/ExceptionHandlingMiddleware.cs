using BuildingBlocks.Domain;
using BuildingBlocks.Contracts;
using BuildingBlocks.Web.Extensions;
using BuildingBlocks.Web.Models;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace Inventory.Api.Middleware;

/// <summary>
/// Converts unhandled exceptions into shared <see cref="ApiErrorResponse"/> responses with the
/// appropriate HTTP status code. Does not log — <c>RequestLoggingMiddleware</c> already logs the
/// exception once as part of the request summary line.
/// </summary>
public sealed class ExceptionHandlingMiddleware : IExceptionHandler
{
    private readonly IErrorCatalog _errorCatalog;
    private readonly IPersistenceExceptionClassifier _persistenceExceptionClassifier;

    /// <summary>
    /// Initializes the middleware with services used to classify and describe exceptions.
    /// </summary>
    /// <param name="errorCatalog">Shared error catalog.</param>
    /// <param name="persistenceExceptionClassifier">Provider-neutral persistence exception classifier.</param>
    public ExceptionHandlingMiddleware(
        IErrorCatalog errorCatalog,
        IPersistenceExceptionClassifier persistenceExceptionClassifier)
    {
        _errorCatalog = errorCatalog;
        _persistenceExceptionClassifier = persistenceExceptionClassifier;
    }

    /// <summary>
    /// Maps an unhandled exception to an HTTP status code and writes a shared API error response.
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="exception">Unhandled exception.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true"/> to indicate the exception was handled and a response was written.</returns>
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var persistenceError = _persistenceExceptionClassifier.Classify(exception);
        int status;
        string code;
        bool retryable;

        if (persistenceError is not null)
        {
            status = 409;
            code = persistenceError.Code;
            retryable = persistenceError.Retryable;
        }
        else if (exception is ValidationException)
        {
            status = 400;
            code = ErrorCodes.Validation;
            retryable = false;
        }
        else if (exception is DomainException)
        {
            status = 400;
            code = ErrorCodes.InvalidOperation;
            retryable = false;
        }
        else if (exception is UnauthorizedAccessException)
        {
            status = 401;
            code = ErrorCodes.Unauthorized;
            retryable = false;
        }
        else if (exception.GetType().Name == "ForbiddenException")
        {
            status = 403;
            code = ErrorCodes.Forbidden;
            retryable = false;
        }
        else if (exception is BadHttpRequestException badRequestException)
        {
            status = badRequestException.StatusCode;
            code = ErrorCodes.InvalidRequest;
            retryable = false;
        }
        else
        {
            status = 500;
            code = ErrorCodes.InternalServerError;
            retryable = false;
        }

        context.Response.StatusCode = status;
        var error = _errorCatalog.Get(code);
        IReadOnlyCollection<ApiError>? errors = null;
        IReadOnlyCollection<ValidationError>? validationErrors = null;

        if (status == 409)
        {
            errors = [new ApiError("retryable", retryable.ToString())];
        }

        if (exception is ValidationException validation)
        {
            validationErrors = validation.ToValidationErrors();
        }

        var response = new ApiErrorResponse(
            status,
            error.Code,
            error.Description,
            context.TraceIdentifier,
            context.GetCorrelationId(),
            errors,
            validationErrors);

        await context.Response.WriteAsJsonAsync(response, ct);
        return true;
    }
}
