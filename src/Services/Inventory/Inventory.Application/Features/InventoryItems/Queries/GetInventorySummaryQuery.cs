using Inventory.Application.Features.InventoryItems.DTOs;

namespace Inventory.Application.Features.InventoryItems.Queries;

/// <summary>
/// Query for aggregated inventory stock-status counts.
/// </summary>
/// <param name="Filter">Aggregation inputs, including the low-stock threshold.</param>
public sealed record GetInventorySummaryQuery(InventorySummaryFilter Filter) : IQuery<InventorySummary>;
