namespace BuildingBlocks.Contracts;

/// <summary>
/// Kafka consumer group ids used across services, centralized here to avoid magic strings.
/// </summary>
public static class KafkaConsumerGroups
{
    /// <summary>Consumer group used by AuditLog.Worker.</summary>
    public const string AuditMongoDb = "audit-mongodb-v3";

    /// <summary>Consumer group used by Inventory.Api for Sales order events.</summary>
    public const string InventoryOrders = "inventory-orders-v1";

    /// <summary>Consumer group used by Sales.Api for Inventory reservation result events.</summary>
    public const string SalesInventoryResults = "sales-inventory-results-v1";
}
