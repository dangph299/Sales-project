using MediatR;

namespace Inventory.Application;

/// <summary>
/// Handles inventory snapshot lookups.
/// </summary>
public sealed class GetInventoryByProductQueryHandler(IInventoryReadService readService)
    : IRequestHandler<GetInventoryByProductQuery, InventorySnapshot?>
{
    /// <inheritdoc/>
    public Task<InventorySnapshot?> Handle(GetInventoryByProductQuery request, CancellationToken cancellationToken)
    {
        return readService.GetAsync(request.ProductId, cancellationToken);
    }
}
