namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Pushes standardized properties around message processing without duplicating logging code in each consumer.
/// </summary>
public interface IMessageLogContext
{
    IDisposable Push(params MessageLogContextProperty[] properties);
}
