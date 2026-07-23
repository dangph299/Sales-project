using System.Security.Claims;
using BuildingBlocks.Web.Extensions;
using Inventory.Api.Extensions;
using Inventory.Api.Models.Requests;
using Inventory.Application.Features.InventoryItems.Commands;
using Inventory.Application.Features.InventoryItems.DTOs;
using Inventory.Application.Features.InventoryItems.Queries;
using Inventory.Application.Features.Reservations.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
    private readonly IOptions<InventorySummaryOptions> _summaryOptions;

    /// <summary>
    /// Initializes the controller with the MediatR sender and inventory summary configuration.
    /// </summary>
    /// <param name="sender">MediatR sender.</param>
    /// <param name="summaryOptions">Server-side configuration for the inventory summary endpoint.</param>
    public InventoryController(ISender sender, IOptions<InventorySummaryOptions> summaryOptions)
    {
        _sender = sender;
        _summaryOptions = summaryOptions;
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
        if (item is null)
        {
            return NotFound();
        }

        return this.ToOkResponse(item);
    }

    /// <summary>
    /// Loads current stock levels for a bounded set of product variants.
    /// </summary>
    /// <param name="body">Product variant ids to load.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with one snapshot per distinct requested variant id.</returns>
    [HttpPost("by-variant-ids")]
    public async Task<IActionResult> GetByVariantIds([FromBody] GetInventoryByVariantIdsRequest? body, CancellationToken ct)
    {
        var items = await _sender.Send(new GetInventoryByProductVariantsQuery(body?.ProductVariantIds), ct);
        return this.ToOkResponse(items);
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
        if (reservation is null)
        {
            return NotFound();
        }

        return this.ToOkResponse(reservation);
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
        return this.ToOkResponse(item);
    }

    /// <summary>
    /// Loads aggregated stock-status counts across tracked inventory items.
    /// </summary>
    /// <param name="query">Optional summary inputs, including the low-stock threshold override.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the aggregated summary.</returns>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] InventorySummaryRequest query, CancellationToken ct)
    {
        var threshold = query.LowStockThreshold ?? _summaryOptions.Value.LowStockThreshold;
        var filter = new InventorySummaryFilter(threshold, query.WarehouseId, query.LocationId, query.CompanyId);
        var summary = await _sender.Send(new GetInventorySummaryQuery(filter), ct);
        return this.ToOkResponse(summary);
    }
}
