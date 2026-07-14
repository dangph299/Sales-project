using BuildingBlocks.Domain;
using BuildingBlocks.Contracts;
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
    private readonly IErrorCatalog _errorCatalog;

    /// <summary>
    /// Initializes the middleware with the service used to write <see cref="ProblemDetails"/> responses.
    /// </summary>
    /// <param name="problemDetails">Problem details service.</param>
    /// <param name="errorCatalog">Shared error catalog.</param>
    public ExceptionHandlingMiddleware(IProblemDetailsService problemDetails, IErrorCatalog errorCatalog)
    {
        _problemDetails = problemDetails;
        _errorCatalog = errorCatalog;
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
        var (status, code, retryable) = exception switch
        {
            // Another request already committed a change to this exact row (EF Core's optimistic
            // concurrency check failed), or two requests raced to create/record the same
            // not-yet-existing row (Postgres unique-key violation). The caller must re-fetch
            // current state before retrying — blindly resubmitting the same payload can silently
            // clobber the other change.
            DbUpdateConcurrencyException => (409, ErrorCodes.ConcurrencyConflict, false),
            DbUpdateException ex when PostgresExceptions.IsUniqueViolation(ex) => (409, ErrorCodes.UniqueViolation, false),
            ValidationException => (400, ErrorCodes.Validation, false),
            DomainException => (400, ErrorCodes.InvalidOperation, false),
            UnauthorizedAccessException => (401, ErrorCodes.Unauthorized, false),
            _ when exception.GetType().Name == "ForbiddenException" => (403, ErrorCodes.Forbidden, false),
            BadHttpRequestException bad => (bad.StatusCode, ErrorCodes.InvalidRequest, false),
            // Inventory's SERIALIZABLE transaction was aborted by a serialization failure or
            // deadlock. This is the only conflict guard for AdjustInventoryCommand, the one
            // Inventory command dispatched over HTTP — nothing was persisted, so the identical
            // request is safe to retry immediately, no re-fetch needed (unlike the concurrency
            // cases above). ReserveStockCommand/ReleaseStockCommand also run inside this kind of
            // transaction but are dispatched only from Kafka via InventoryIntegrationEventProcessor
            // and never reach this middleware; a conflict there is instead retried transparently by
            // Kafka's own at-least-once redelivery, independent of this mapping. Any other
            // DbUpdateException (FK violation, NOT NULL violation, etc.) is a genuine data/schema
            // defect, not a transient conflict, so it falls through to the 500 case below instead
            // of being reported as retryable.
            _ when PostgresExceptions.IsSerializationConflict(exception) => (409, ErrorCodes.ConcurrencyConflict, true),
            _ => (500, ErrorCodes.InternalServerError, false)
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
        if (exception is ValidationException validation)
            details.Extensions["errors"] = validation.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
        if (status == 409) details.Extensions["retryable"] = retryable;
        details.Extensions["traceId"] = context.TraceIdentifier;
        return await _problemDetails.TryWriteAsync(new() { HttpContext = context, ProblemDetails = details });
    }
}
