using KafkaFlow;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Shared lifecycle helper for starting and stopping the KafkaFlow bus.
/// </summary>
public static class KafkaBusLifecycle
{
    /// <summary>
    /// Creates and starts the Kafka bus from the given service provider.
    /// </summary>
    public static async Task<IKafkaBus> StartAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var bus = services.CreateKafkaBus();
        await bus.StartAsync(cancellationToken);
        return bus;
    }

    /// <summary>
    /// Stops the Kafka bus when one has been started.
    /// </summary>
    public static Task StopAsync(IKafkaBus? bus, CancellationToken cancellationToken = default)
    {
        return bus?.StopAsync() ?? Task.CompletedTask;
    }
}
