namespace BuildingBlocks.Application;

/// <summary>
/// Provides the current UTC time for application services without coupling them to the system clock.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
