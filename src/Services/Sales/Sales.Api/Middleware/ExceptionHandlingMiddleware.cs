using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales.Application;
using Sales.Domain;

namespace Sales.Api.Middleware;

/// <summary>
/// Converts unhandled exceptions into RFC 7807 <see cref="ProblemDetails"/> responses with the
/// appropriate HTTP status code. Does not log — <c>RequestLoggingMiddleware</c> already logs the
/// exception once as part of the request summary line.
/// </summary>
public sealed class ExceptionHandlingMiddleware : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;

    /// <summary>
    /// Initializes the middleware with the service used to write <see cref="ProblemDetails"/> responses.
    /// </summary>
    /// <param name="problemDetails">
    /// The problem details service.
    /// </param>
    public ExceptionHandlingMiddleware(IProblemDetailsService problemDetails)
    {
        _problemDetails = problemDetails;
    }

    /// <summary>
    /// Maps an unhandled exception to an HTTP status code and writes a <see cref="ProblemDetails"/> response.
    /// </summary>
    /// <param name="context">
    /// The current HTTP context.
    /// </param>
    /// <param name="exception">
    /// The unhandled exception.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// <see langword="true"/> to indicate the exception was handled and a response was written.
    /// </returns>
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (404, "Resource not found"),
            ConflictException => (409, "Concurrent update detected"),
            DbUpdateConcurrencyException => (409, "Concurrent update detected"),
            ValidationException => (400, "Validation failed"),
            DomainException => (400, "Domain rule violated"),
            BadHttpRequestException bad => (bad.StatusCode, "Invalid request"),
            _ => (500, "Unexpected server error")
        };
        context.Response.StatusCode = status;
        var details = new ProblemDetails { Status = status, Title = title, Detail = exception.Message, Instance = context.Request.Path };
        if (exception is ConflictException conflict) details.Extensions["currentVersion"] = conflict.CurrentVersion;
        if (exception is ValidationException validation)
            details.Extensions["errors"] = validation.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
        details.Extensions["traceId"] = context.TraceIdentifier;
        return await _problemDetails.TryWriteAsync(new() { HttpContext = context, ProblemDetails = details });
    }
}
