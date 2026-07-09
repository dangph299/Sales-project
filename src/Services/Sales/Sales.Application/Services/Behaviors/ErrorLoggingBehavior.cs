using System.Diagnostics;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Sales.Domain;

namespace Sales.Application;

/// <summary>
/// The single place a command/query failure is logged. Handlers should not catch-and-log their
/// own exceptions - this behavior guarantees exactly one log per failure regardless of how many
/// handlers exist, so business handlers only need to log Start/StateChange/Success.
/// </summary>
/// <typeparam name="TRequest">
/// The MediatR request type being handled.
/// </typeparam>
/// <typeparam name="TResponse">
/// The response type returned by the request handler.
/// </typeparam>
public sealed class ErrorLoggingBehavior<TRequest, TResponse>(ILogger<ErrorLoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Invokes the next handler in the pipeline, logging any failure exactly once at Warning
    /// (expected business-rule violations) or Error (unexpected failures) before rethrowing.
    /// </summary>
    /// <param name="request">
    /// The request being handled.
    /// </param>
    /// <param name="next">
    /// The next delegate in the pipeline.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The response produced by <paramref name="next"/>.
    /// </returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await next(cancellationToken);
        }
        catch (Exception ex) when (IsBusinessRuleViolation(ex))
        {
            logger.LogWarning(ex, "{RequestName} rejected {ElapsedMs} {@Request}", typeof(TRequest).Name, sw.ElapsedMilliseconds, request);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{RequestName} failed {ElapsedMs} {@Request}", typeof(TRequest).Name, sw.ElapsedMilliseconds, request);
            throw;
        }
    }

    private static bool IsBusinessRuleViolation(Exception ex) =>
        ex is ValidationException or DomainException or NotFoundException or ConflictException || IsOptimisticConcurrencyConflict(ex);

    // EF Core's DbUpdateConcurrencyException is an Infrastructure type; Application must not reference
    // Microsoft.EntityFrameworkCore (enforced by Sales.Architecture.Tests), so match by type name instead.
    // ExceptionHandlingMiddleware already treats it identically to ConflictException - a routine 409.
    private static bool IsOptimisticConcurrencyConflict(Exception ex) =>
        ex.GetType().FullName == "Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException";
}
