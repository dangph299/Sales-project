using BuildingBlocks.Contracts;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Records failed inbound Kafka message attempts in a service inbox.
/// </summary>
public interface IInboxFailureRecorder
{
    /// <summary>
    /// Records a failed processing attempt and returns whether the message has been dead-lettered.
    /// </summary>
    Task<InboundFailureResult> RecordFailureAsync(
        EventEnvelope envelope,
        InboundMessageContext context,
        Exception exception,
        CancellationToken cancellationToken = default);
}
