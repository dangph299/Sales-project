namespace BuildingBlocks.Contracts;

/// <summary>
/// Kafka topic names used across services, centralized here to avoid magic strings. Follows the
/// <c>&lt;bounded-context&gt;.&lt;business-event&gt;.v&lt;version&gt;</c> naming convention.
/// </summary>
public static class KafkaTopics
{
    /// <summary>Topic Sales publishes <see cref="AuditLogEvent"/> events to.</summary>
    public const string SalesAudit = "sales.audit.v1";

    /// <summary>Topic Inventory publishes <see cref="AuditLogEvent"/> events to.</summary>
    public const string InventoryAudit = "inventory.audit.v1";

    /// <summary>Topic Sales publishes <see cref="OrderConfirmationRequested"/> events to.</summary>
    public const string OrderConfirmationRequested = "sales.order-confirmation-requested.v1";

    /// <summary>Topic Sales publishes <see cref="OrderUndoConfirmationRequested"/> events to.</summary>
    public const string OrderUndoConfirmationRequested = "sales.order-undo-confirmation-requested.v1";

    /// <summary>Topic Inventory publishes <see cref="StockReserved"/> events to.</summary>
    public const string StockReserved = "inventory.stock-reserved.v1";

    /// <summary>Topic Inventory publishes <see cref="StockRejected"/> events to.</summary>
    public const string StockRejected = "inventory.stock-rejected.v1";

    /// <summary>Topic Inventory publishes <see cref="StockReleased"/> events to.</summary>
    public const string StockReleased = "inventory.stock-released.v1";
}
