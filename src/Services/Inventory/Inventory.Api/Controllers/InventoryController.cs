using System.Security.Claims;
using Inventory.Api.Models.Requests;
using Inventory.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

/// <summary>
/// HTTP API for reading inventory state and manually adjusting stock. Delegates use cases to the
/// Inventory application service.
/// </summary>
[ApiController]
[Authorize]
[Route("api/inventory")]
public sealed class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventory;

    /// <summary>
    /// Initializes the controller with the Inventory application service.
    /// </summary>
    /// <param name="inventory">
    /// The Inventory application service.
    /// </param>
    public InventoryController(IInventoryService inventory)
    {
        _inventory = inventory;
    }

    /// <summary>
    /// Loads current stock levels for a product.
    /// </summary>
    /// <param name="productId">
    /// The unique identifier of the product to load.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// <c>200 OK</c> with the stock snapshot, or <c>404 Not Found</c> if no inventory item exists.
    /// </returns>
    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> Get(Guid productId, CancellationToken ct)
    {
        var item = await _inventory.GetAsync(productId, ct);
        return item is null
            ? NotFound()
            : Ok(new { item.ProductId, item.Sku, item.Available, item.Reserved, item.Version });
    }

    /// <summary>
    /// Loads the inventory reservation associated with a Sales order.
    /// </summary>
    /// <param name="orderId">
    /// The unique identifier of the Sales order.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// <c>200 OK</c> with the reservation snapshot, or <c>404 Not Found</c> if none exists.
    /// </returns>
    [HttpGet("reservations/{orderId:guid}")]
    public async Task<IActionResult> GetReservation(Guid orderId, CancellationToken ct)
    {
        var reservation = await _inventory.GetReservationAsync(orderId, ct);
        return reservation is null ? NotFound() : Ok(reservation);
    }

    /// <summary>
    /// Manually adjusts the available stock for a product.
    /// </summary>
    /// <param name="productId">
    /// The unique identifier of the product to adjust.
    /// </param>
    /// <param name="body">
    /// The SKU and signed quantity delta to apply.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// <c>200 OK</c> with the adjusted stock snapshot.
    /// </returns>
    [HttpPost("{productId:guid}/adjust")]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<IActionResult> Adjust(Guid productId, [FromBody] AdjustStockRequest body, CancellationToken ct)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var item = await _inventory.AdjustAsync(productId, body.Sku, body.QuantityDelta, actor, ct);
        return Ok(new { item.ProductId, item.Sku, item.Available, item.Reserved, item.Version });
    }
}
