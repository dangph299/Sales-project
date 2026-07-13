namespace BuildingBlocks.Contracts;

/// <summary>
/// Generic event envelope for new contracts that can keep the payload strongly typed.
/// </summary>
public sealed record EventEnvelope<TMessage>(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    EventMetadata Metadata,
    TMessage Payload);
