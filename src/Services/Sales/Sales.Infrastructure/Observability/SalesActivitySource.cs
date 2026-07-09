using System.Diagnostics;

namespace Sales.Infrastructure;

/// <summary>
/// The <see cref="ActivitySource"/> used to trace Sales' Kafka publish/consume operations. Must be
/// registered via <c>AddSource(Name)</c> on the tracing provider for its spans to be exported.
/// </summary>
internal static class SalesActivitySource
{
    /// <summary>
    /// The name under which this activity source must be registered with the tracing provider.
    /// </summary>
    public const string Name = "Sales.Infrastructure.Kafka";

    /// <summary>
    /// The shared <see cref="ActivitySource"/> instance used to start Kafka publish/consume spans.
    /// </summary>
    public static readonly ActivitySource Instance = new(Name);
}
