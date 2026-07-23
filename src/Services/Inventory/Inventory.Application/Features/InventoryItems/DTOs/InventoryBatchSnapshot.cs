namespace Inventory.Application.Features.InventoryItems.DTOs;

/// <summary>
/// Batch inventory response keyed by product variant id.
/// </summary>
/// <param name="Items">Inventory snapshots for the requested product variant ids.</param>
public sealed record InventoryBatchSnapshot(IReadOnlyCollection<InventorySnapshot> Items);
