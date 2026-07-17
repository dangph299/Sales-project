using Inventory.Application.Features.InventoryItems.DTOs;
using Inventory.Domain;
using Mapster;

namespace Inventory.Application.Features.InventoryItems.Mapping;

/// <summary>
/// Mapster configuration for the InventoryItem aggregate's read models.
/// </summary>
public sealed class InventoryItemMappingRegister : IRegister
{
    /// <inheritdoc/>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<InventoryItem, InventorySnapshot>();
    }
}
