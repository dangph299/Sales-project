using Inventory.Application.Features.InventoryItems.DTOs;
using Inventory.Application.Features.InventoryItems.Interfaces;
using MediatR;

namespace Inventory.Application.Features.InventoryItems.Queries;

/// <summary>
/// Handles inventory snapshot lookups.
/// </summary>
public sealed class GetInventoryByProductQueryHandler(IInventoryItemReadService readService)
    : IRequestHandler<GetInventoryByProductQuery, InventorySnapshot?>
{
    /// <inheritdoc/>
    public Task<InventorySnapshot?> Handle(GetInventoryByProductQuery request, CancellationToken cancellationToken)
    {
        return readService.GetAsync(request.ProductId, cancellationToken);
    }
}
