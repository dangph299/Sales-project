using MediatR;
using Microsoft.Extensions.Logging;

namespace Sales.Application;

/// <summary>
/// Logs Debug-level progress markers (started/completed/failed) around every command/query, useful
/// for tracing execution without adding Information-level noise. Failure logging for triage is
/// owned by <see cref="ErrorLoggingBehavior{TRequest,TResponse}"/>, not this behavior.
/// </summary>
/// <typeparam name="TRequest">
/// The MediatR request type being handled.
/// </typeparam>
/// <typeparam name="TResponse">
/// The response type returned by the request handler.
/// </typeparam>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Invokes the next handler in the pipeline, logging Debug-level start/completion/failure markers.
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
        var requestName = typeof(TRequest).Name;
        logger.LogDebug("Pipeline started {RequestName}", requestName);
        try
        {
            var response = await next(cancellationToken);
            logger.LogDebug("Pipeline completed {RequestName}", requestName);
            return response;
        }
        catch (Exception ex)
        {
            // Debug only: the owning handler is responsible for the single Error log per failure.
            logger.LogDebug(ex, "Pipeline failed {RequestName}", requestName);
            throw;
        }
    }
}
