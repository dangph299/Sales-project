using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Application;

/// <summary>
/// Logs Debug-level progress markers around every command/query.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc/>
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
            logger.LogDebug(ex, "Pipeline failed {RequestName}", requestName);
            throw;
        }
    }
}
