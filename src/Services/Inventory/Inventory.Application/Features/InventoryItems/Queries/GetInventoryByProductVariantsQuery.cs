using Inventory.Application.Features.InventoryItems.DTOs;

namespace Inventory.Application.Features.InventoryItems.Queries;

/// <summary>
/// Query for inventory snapshots for multiple product variants.
/// </summary>
/// <param name="ProductVariantIds">Product variant identifiers to load.</param>
public sealed record GetInventoryByProductVariantsQuery(IReadOnlyCollection<Guid>? ProductVariantIds)
    : IQuery<InventoryBatchSnapshot>;
