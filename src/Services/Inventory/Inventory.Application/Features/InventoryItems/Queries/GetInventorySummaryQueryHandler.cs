using Inventory.Application.Features.InventoryItems.DTOs;
using Inventory.Application.Features.InventoryItems.Interfaces;
using MediatR;

namespace Inventory.Application.Features.InventoryItems.Queries;

/// <summary>
/// Handles inventory summary aggregation lookups.
/// </summary>
public sealed class GetInventorySummaryQueryHandler(IInventoryItemReadService readService)
    : IRequestHandler<GetInventorySummaryQuery, InventorySummary>
{
    /// <inheritdoc/>
    public Task<InventorySummary> Handle(GetInventorySummaryQuery request, CancellationToken cancellationToken)
    {
        return readService.GetSummaryAsync(request.Filter, cancellationToken);
    }
}
