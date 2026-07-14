using System.Security.Claims;
using Inventory.Api.Models.Requests;
using Inventory.Application;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

/// <summary>
/// HTTP API for reading inventory state and manually adjusting stock. Delegates use cases through
/// MediatR.
/// </summary>
[ApiController]
[Authorize]
[Route("api/inventory")]
public sealed class InventoryController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes the controller with the MediatR sender.
    /// </summary>
    /// <param name="sender">MediatR sender.</param>
    public InventoryController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Loads current stock levels for a product.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the stock snapshot, or <c>404 Not Found</c> if no inventory item exists.</returns>
    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> Get(Guid productId, CancellationToken ct)
    {
        var item = await _sender.Send(new GetInventoryByProductQuery(productId), ct);
        return item is null
            ? NotFound()
            : Ok(new { item.ProductId, item.Sku, item.Available, item.Reserved, item.Version });
    }

    /// <summary>
    /// Loads the inventory reservation associated with a Sales order.
    /// </summary>
    /// <param name="orderId">Sales order.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the reservation snapshot, or <c>404 Not Found</c> if none exists.</returns>
    [HttpGet("reservations/{orderId:guid}")]
    public async Task<IActionResult> GetReservation(Guid orderId, CancellationToken ct)
    {
        var reservation = await _sender.Send(new GetReservationByOrderQuery(orderId), ct);
        return reservation is null ? NotFound() : Ok(reservation);
    }

    /// <summary>
    /// Manually adjusts the available stock for a product.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="body">SKU and signed quantity delta to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the adjusted stock snapshot.</returns>
    [HttpPost("{productId:guid}/adjust")]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<IActionResult> Adjust(Guid productId, [FromBody] AdjustStockRequest body, CancellationToken ct)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var item = await _sender.Send(new AdjustInventoryCommand(productId, body.Sku, body.QuantityDelta, actor), ct);
        return Ok(new { item.ProductId, item.Sku, item.Available, item.Reserved, item.Version });
    }
}
