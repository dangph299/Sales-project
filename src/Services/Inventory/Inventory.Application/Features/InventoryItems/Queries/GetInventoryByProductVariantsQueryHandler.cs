using Inventory.Application.Features.InventoryItems.DTOs;
using Inventory.Application.Features.InventoryItems.Interfaces;

namespace Inventory.Application.Features.InventoryItems.Queries;

/// <summary>
/// Handles batch inventory snapshot lookups.
/// </summary>
public sealed class GetInventoryByProductVariantsQueryHandler(IInventoryItemReadService readService)
    : IQueryHandler<GetInventoryByProductVariantsQuery, InventoryBatchSnapshot>
{
    /// <inheritdoc/>
    public async Task<InventoryBatchSnapshot> Handle(
        GetInventoryByProductVariantsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await readService.GetByProductVariantIdsAsync(request.ProductVariantIds ?? [], cancellationToken);
        return new InventoryBatchSnapshot(items);
    }
}
