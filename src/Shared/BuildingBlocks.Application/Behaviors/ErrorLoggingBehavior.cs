using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Application;

/// <summary>
/// Logs command/query failures once at Warning for expected rejections or Error for unexpected failures.
/// </summary>
public sealed class ErrorLoggingBehavior<TRequest, TResponse>(
    ILogger<ErrorLoggingBehavior<TRequest, TResponse>> logger,
    IApplicationExceptionClassifier exceptionClassifier) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc/>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await next(cancellationToken);
        }
        catch (Exception ex) when (exceptionClassifier.IsExpected(ex))
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
}
