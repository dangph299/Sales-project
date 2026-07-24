using Inventory.Application.Features.InventoryItems.DTOs;
using Inventory.Application.Features.InventoryItems.Interfaces;

namespace Inventory.Application.Features.InventoryItems.Queries;

/// <summary>
/// Handles inventory summary aggregation lookups.
/// </summary>
public sealed class GetInventorySummaryQueryHandler(IInventoryItemReadService readService)
    : IQueryHandler<GetInventorySummaryQuery, InventorySummary>
{
    /// <inheritdoc/>
    public Task<InventorySummary> Handle(GetInventorySummaryQuery request, CancellationToken cancellationToken)
    {
        return readService.GetSummaryAsync(request.Filter, cancellationToken);
    }
}
