using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Middleware;

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
    /// <param name="problemDetails">Problem details service.</param>
    public ExceptionHandlingMiddleware(IProblemDetailsService problemDetails)
    {
        _problemDetails = problemDetails;
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
        var (status, title) = exception switch
        {
            // A concurrent request changed the same aggregate (EF Core's optimistic concurrency
            // check failed), or two requests raced to create/record the same not-yet-existing row
            // (Postgres unique-key violation). Both mean "retry the request". Any other
            // DbUpdateException (FK violation, NOT NULL violation, etc.) is a genuine data/schema
            // defect, not a transient conflict, so it falls through to the 500 case below instead
            // of being reported as retryable.
            DbUpdateConcurrencyException => (409, "Concurrent update detected"),
            DbUpdateException ex when PostgresExceptions.IsUniqueViolation(ex) => (409, "Concurrent update detected"),
            ValidationException => (400, "Validation failed"),
            DomainException => (400, "Domain rule violated"),
            BadHttpRequestException bad => (bad.StatusCode, "Invalid request"),
            _ => (500, "Unexpected server error")
        };
        context.Response.StatusCode = status;
        var details = new ProblemDetails { Status = status, Title = title, Detail = exception.Message, Instance = context.Request.Path };
        if (exception is ValidationException validation)
            details.Extensions["errors"] = validation.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
        details.Extensions["traceId"] = context.TraceIdentifier;
        return await _problemDetails.TryWriteAsync(new() { HttpContext = context, ProblemDetails = details });
    }
}
