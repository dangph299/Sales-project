namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Result of recording a failed inbound message attempt.
/// </summary>
public sealed record InboundFailureResult(int Attempts, bool DeadLettered);
