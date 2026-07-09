using System.Diagnostics;

namespace AuditLog.Infrastructure;

/// <summary>
/// The <see cref="ActivitySource"/> used to trace AuditLog's Kafka consume operations. Must be
/// registered via <c>AddSource(Name)</c> on the tracing provider for its spans to be exported.
/// </summary>
internal static class AuditActivitySource
{
    /// <summary>
    /// The name under which this activity source must be registered with the tracing provider.
    /// </summary>
    public const string Name = "AuditLog.Infrastructure.Kafka";

    /// <summary>
    /// The shared <see cref="ActivitySource"/> instance used to start Kafka consume spans.
    /// </summary>
    public static readonly ActivitySource Instance = new(Name);
}
