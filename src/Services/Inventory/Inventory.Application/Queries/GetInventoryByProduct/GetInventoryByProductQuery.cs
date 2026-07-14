namespace Inventory.Application;

/// <summary>
/// Query for one product's inventory snapshot.
/// </summary>
/// <param name="ProductId">Product identifier.</param>
public sealed record GetInventoryByProductQuery(Guid ProductId) : IQuery<InventorySnapshot?>;
