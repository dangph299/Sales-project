using BuildingBlocks.Contracts;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Applies a consumed integration event to local service state.
/// </summary>
public interface IIntegrationEventProcessor
{
    /// <summary>
    /// Processes an event and returns a short outcome for logs and metrics.
    /// </summary>
    Task<string> ProcessAsync(EventEnvelope envelope);
}
