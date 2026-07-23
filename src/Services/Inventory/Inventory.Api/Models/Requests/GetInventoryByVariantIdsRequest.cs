namespace Inventory.Api.Models.Requests;

/// <summary>
/// Request body for loading inventory snapshots for a bounded set of product variants.
/// </summary>
/// <param name="ProductVariantIds">Product variant identifiers to load.</param>
public sealed record GetInventoryByVariantIdsRequest(IReadOnlyCollection<Guid>? ProductVariantIds);
