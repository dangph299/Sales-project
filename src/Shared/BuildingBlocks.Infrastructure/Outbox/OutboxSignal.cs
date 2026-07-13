using System.Threading.Channels;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// In-process signal for reducing outbox publish latency without relying only on polling.
/// </summary>
public sealed class OutboxSignal : IOutboxSignal
{
    private readonly Channel<bool> channel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = false,
        SingleWriter = false
    });

    /// <inheritdoc />
    public void Notify() => channel.Writer.TryWrite(true);

    /// <inheritdoc />
    public async Task WaitAsync(TimeSpan fallbackInterval, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(fallbackInterval);

        try
        {
            if (!await channel.Reader.WaitToReadAsync(timeout.Token)) return;
            while (channel.Reader.TryRead(out _)) { }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Polling fallback elapsed.
        }
    }
}
