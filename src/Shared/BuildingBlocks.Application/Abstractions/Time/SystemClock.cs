namespace BuildingBlocks.Application;

/// <summary>
/// System-backed UTC clock for production use.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
