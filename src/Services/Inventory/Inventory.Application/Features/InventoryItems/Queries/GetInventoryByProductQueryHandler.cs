using Inventory.Application.Features.InventoryItems.DTOs;
using Inventory.Application.Features.InventoryItems.Interfaces;

namespace Inventory.Application.Features.InventoryItems.Queries;

/// <summary>
/// Handles inventory snapshot lookups.
/// </summary>
public sealed class GetInventoryByProductQueryHandler(IInventoryItemReadService readService)
    : IQueryHandler<GetInventoryByProductQuery, InventorySnapshot?>
{
    /// <inheritdoc/>
    public Task<InventorySnapshot?> Handle(GetInventoryByProductQuery request, CancellationToken cancellationToken)
    {
        return readService.GetAsync(request.ProductId, cancellationToken);
    }
}
