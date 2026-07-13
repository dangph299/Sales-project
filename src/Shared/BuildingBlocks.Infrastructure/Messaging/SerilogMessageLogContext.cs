using Serilog.Context;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Serilog-backed message log context implementation.
/// </summary>
public sealed class SerilogMessageLogContext : IMessageLogContext
{
    public IDisposable Push(params MessageLogContextProperty[] properties)
    {
        if (properties.Length == 0)
        {
            return EmptyDisposable.Instance;
        }

        return LogContext.Push(properties.Select(x => new Serilog.Core.Enrichers.PropertyEnricher(x.Name, x.Value)).ToArray());
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
