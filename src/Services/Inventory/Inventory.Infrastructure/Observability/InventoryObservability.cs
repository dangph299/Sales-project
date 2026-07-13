namespace Inventory.Infrastructure;

/// <summary>
/// Well-known observability names owned by the Inventory service.
/// </summary>
public static class InventoryObservability
{
    /// <summary>
    /// Activity source name for Inventory Kafka publish/consume operations.
    /// </summary>
    public const string KafkaActivitySourceName = "Inventory.Infrastructure.Kafka";
}
