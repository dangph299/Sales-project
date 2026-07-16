namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Transport metadata captured when an inbound Kafka message fails processing.
/// </summary>
public sealed record InboundMessageContext(
    string Topic,
    string GroupId,
    int Partition,
    long Offset);
