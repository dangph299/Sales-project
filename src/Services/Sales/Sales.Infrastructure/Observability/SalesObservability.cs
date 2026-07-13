namespace Sales.Infrastructure;

/// <summary>
/// Well-known observability names owned by the Sales service.
/// </summary>
public static class SalesObservability
{
    /// <summary>
    /// Activity source name for Sales Kafka publish/consume operations.
    /// </summary>
    public const string KafkaActivitySourceName = "Sales.Infrastructure.Kafka";
}
