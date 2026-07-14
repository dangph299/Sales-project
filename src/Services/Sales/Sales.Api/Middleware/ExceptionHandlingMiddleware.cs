using FluentValidation;
using BuildingBlocks.Contracts;
using BuildingBlocks.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Sales.Application;

namespace Sales.Api.Middleware;

/// <summary>
/// Converts unhandled exceptions into RFC 7807 <see cref="ProblemDetails"/> responses with the
/// appropriate HTTP status code. Does not log — <c>RequestLoggingMiddleware</c> already logs the
/// exception once as part of the request summary line.
/// </summary>
public sealed class ExceptionHandlingMiddleware : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly IErrorCatalog _errorCatalog;
    private readonly IPersistenceExceptionClassifier _persistenceExceptionClassifier;

    /// <summary>
    /// Initializes the middleware with the service used to write <see cref="ProblemDetails"/> responses.
    /// </summary>
    /// <param name="problemDetails">Problem details service.</param>
    /// <param name="errorCatalog">Shared error catalog.</param>
    /// <param name="persistenceExceptionClassifier">Provider-neutral persistence exception classifier.</param>
    public ExceptionHandlingMiddleware(
        IProblemDetailsService problemDetails,
        IErrorCatalog errorCatalog,
        IPersistenceExceptionClassifier persistenceExceptionClassifier)
    {
        _problemDetails = problemDetails;
        _errorCatalog = errorCatalog;
        _persistenceExceptionClassifier = persistenceExceptionClassifier;
    }

    /// <summary>
    /// Maps an unhandled exception to an HTTP status code and writes a <see cref="ProblemDetails"/> response.
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="exception">Unhandled exception.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true"/> to indicate the exception was handled and a response was written.</returns>
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var persistenceError = _persistenceExceptionClassifier.Classify(exception);
        var (status, code) = persistenceError is not null
            ? (409, persistenceError.Code)
            : exception switch
        {
            NotFoundException => (404, ErrorCodes.NotFound),
            ConflictException => (409, ErrorCodes.ConcurrencyConflict),
            ValidationException => (400, ErrorCodes.Validation),
            DomainException => (400, ErrorCodes.InvalidOperation),
            UnauthorizedAccessException => (401, ErrorCodes.Unauthorized),
            _ when exception.GetType().Name == "ForbiddenException" => (403, ErrorCodes.Forbidden),
            BadHttpRequestException bad => (bad.StatusCode, ErrorCodes.InvalidRequest),
            _ => (500, ErrorCodes.InternalServerError)
        };
        context.Response.StatusCode = status;
        var error = _errorCatalog.Get(code);
        var details = new ProblemDetails
        {
            Status = status,
            Title = error.Description,
            Instance = context.Request.Path
        };
        details.Extensions["code"] = error.Code;
        details.Extensions["description"] = error.Description;
        if (exception is ConflictException conflict) details.Extensions["currentVersion"] = conflict.CurrentVersion;
        if (exception is ValidationException validation)
            details.Extensions["errors"] = validation.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
        details.Extensions["traceId"] = context.TraceIdentifier;
        return await _problemDetails.TryWriteAsync(new() { HttpContext = context, ProblemDetails = details });
    }
}
