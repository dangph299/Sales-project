using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Application;

/// <summary>
/// Logs Debug-level progress markers around every command/query.
/// <para>
/// This deliberately does not log failures at Warning or Error. Every execution path that dispatches
/// MediatR already logs its own failures once, at its own boundary and with the context only that
/// boundary has: <c>ApiExceptionHandler</c> for HTTP (with the public error code and status),
/// <c>IntegrationEventHandler</c> for Kafka (with topic, partition, and offset), the outbox/inbox
/// services for their cycles, and Hangfire for jobs. Re-logging the same exception here would double
/// every failure in Seq and break error-rate counting, so this stays a Debug-level breadcrumb that
/// adds the request payload the boundary loggers do not carry.
/// </para>
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc/>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startedAt = Stopwatch.GetTimestamp();
        logger.LogDebug("Pipeline started {RequestName}", requestName);
        try
        {
            var response = await next(cancellationToken);
            logger.LogDebug(
                "Pipeline completed {RequestName} {ElapsedMs}",
                requestName,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            // {@Request} destructures the whole command, which can carry input the masking policy in
            // RequestObservabilityMiddleware would redact. Keeping it at Debug keeps it out of
            // production sinks, matching how request bodies are handled.
            logger.LogDebug(
                ex,
                "Pipeline failed {RequestName} {ElapsedMs} {@Request}",
                requestName,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
                request);
            throw;
        }
    }
}
