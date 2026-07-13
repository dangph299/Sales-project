using BuildingBlocks.Infrastructure;
using KafkaFlow;

namespace AuditLog.Worker;

/// <summary>
/// Hosted service owning the KafkaFlow bus lifecycle for the worker, starting consumption on host
/// startup and stopping it cleanly on shutdown.
/// </summary>
public sealed class KafkaBusService(IServiceProvider services) : IHostedService
{
    private IKafkaBus? _bus;

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _bus = await KafkaBusLifecycle.StartAsync(services, cancellationToken);
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => KafkaBusLifecycle.StopAsync(_bus, cancellationToken);
}
