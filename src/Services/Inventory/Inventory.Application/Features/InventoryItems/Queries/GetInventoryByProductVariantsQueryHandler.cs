using Inventory.Application.Features.InventoryItems.DTOs;
using Inventory.Application.Features.InventoryItems.Interfaces;
using MediatR;

namespace Inventory.Application.Features.InventoryItems.Queries;

/// <summary>
/// Handles batch inventory snapshot lookups.
/// </summary>
public sealed class GetInventoryByProductVariantsQueryHandler(IInventoryItemReadService readService)
    : IRequestHandler<GetInventoryByProductVariantsQuery, InventoryBatchSnapshot>
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
