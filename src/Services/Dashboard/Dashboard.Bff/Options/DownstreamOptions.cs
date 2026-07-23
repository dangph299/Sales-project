namespace Dashboard.Bff.Options;

/// <summary>
/// Base URLs for the downstream services the Dashboard BFF aggregates data from.
/// </summary>
public sealed class DownstreamOptions
{
    public const string SectionName = "Downstream";

    /// <summary>Base URL of the Sales API.</summary>
    public string SalesBaseUrl { get; set; } = "";

    /// <summary>Base URL of the Inventory API.</summary>
    public string InventoryBaseUrl { get; set; } = "";
}
