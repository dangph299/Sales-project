using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Application;

/// <summary>
/// Logs slow requests at Warning without changing request behavior.
/// </summary>
public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
    TimeSpan? warningThreshold = null) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly TimeSpan _warningThreshold = warningThreshold ?? TimeSpan.FromMilliseconds(500);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();
        var response = await next(cancellationToken);
        var elapsed = Stopwatch.GetElapsedTime(startedAt);

        if (elapsed >= _warningThreshold)
        {
            logger.LogWarning("Slow request {RequestName} completed in {ElapsedMs} ms", typeof(TRequest).Name, elapsed.TotalMilliseconds);
        }

        return response;
    }
}
