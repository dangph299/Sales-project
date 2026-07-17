namespace Sales.Application.Common.Interfaces;

/// <summary>
/// Ambient information about the current request, used to stamp domain/integration events with
/// who caused them and how to correlate them.
/// </summary>
public interface IExecutionContext
{
    /// <summary>
    /// Gets the identifier of the user or system responsible for the current operation.
    /// </summary>
    string Actor { get; }

    /// <summary>
    /// Gets the correlation identifier used to trace the current request/workflow across services.
    /// </summary>
    Guid CorrelationId { get; }
}
