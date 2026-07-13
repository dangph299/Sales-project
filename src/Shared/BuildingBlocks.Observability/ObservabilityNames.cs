namespace BuildingBlocks.Observability;

/// <summary>
/// Centralizes the well-known tracing source names shared between a service's DI registration of its
/// <see cref="System.Diagnostics.ActivitySource"/> and its OpenTelemetry <c>AddSource(...)</c> call, so
/// the two never drift apart. A mismatch between them causes spans to be silently dropped with no error.
/// </summary>
public static class ObservabilityNames
{
    /// <summary>
    /// tracing source name for Sales' Kafka publish/consume operations.
    /// </summary>
    public const string SalesKafka = "Sales.Infrastructure.Kafka";

    /// <summary>
    /// tracing source name for Inventory's Kafka publish/consume operations.
    /// </summary>
    public const string InventoryKafka = "Inventory.Infrastructure.Kafka";
}
