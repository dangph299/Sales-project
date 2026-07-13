using System.Diagnostics;

namespace AuditLog.Infrastructure;

internal static class AuditActivitySource
{
    /// <summary>
    /// name under which this activity source must be registered with the tracing provider.
    /// </summary>
    public const string Name = "AuditLog.Infrastructure.Kafka";

    /// <summary>
    /// shared <see cref="ActivitySource"/> instance used to start Kafka consume spans.
    /// </summary>
    public static readonly ActivitySource Instance = new(Name);
}
