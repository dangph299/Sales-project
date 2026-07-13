namespace BuildingBlocks.Infrastructure;

/// <summary>
/// A structured property pushed into the message processing log context.
/// </summary>
public sealed record MessageLogContextProperty(string Name, object? Value);
