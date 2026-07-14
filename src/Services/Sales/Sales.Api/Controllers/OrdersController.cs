using MediatR;
using BuildingBlocks.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sales.Api.Extensions;
using Sales.Application;

namespace Sales.Api.Controllers;

/// <summary>
/// HTTP API for creating, searching, and progressing orders through their lifecycle. Dispatches to
/// Application via MediatR. Mutating endpoints that touch an existing order require an
/// <c>If-Match</c> request header for optimistic concurrency (see <see cref="ControllerEtagExtensions"/>).
/// </summary>
[ApiController]
[Authorize(Roles = "Admin,Sales")]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes the controller with the MediatR sender used to dispatch commands and queries.
    /// </summary>
    /// <param name="sender">MediatR sender.</param>
    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates a new draft order.
    /// </summary>
    /// <param name="command">Customer and requested lines for the new order.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>201 Created</c> with the created order, and an <c>ETag</c> response header set to its version.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrder command, CancellationToken ct)
    {
        var order = await _sender.Send(command, ct);
        Response.SetEtag(order);
        return this.ToCreatedResponse("/api/orders", order);
    }

    /// <summary>
    /// Searches orders by creation date range and/or customer name/phone.
    /// </summary>
    /// <param name="from">An optional inclusive lower bound on the order's creation time.</param>
    /// <param name="to">An optional inclusive upper bound on the order's creation time.</param>
    /// <param name="customer">An optional substring to match against the order's customer name or phone.</param>
    /// <param name="page">1-based page number. Defaults to 1.</param>
    /// <param name="pageSize">Maximum page size. Defaults to 20.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with a page of matching orders.</returns>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? customer,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var orders = await _sender.Send(new SearchOrders(from, to, customer, page, pageSize), ct);
        return this.ToOkResponse(orders);
    }

    /// <summary>
    /// Loads a single order by its identifier.
    /// </summary>
    /// <param name="id">Order identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the order, and an <c>ETag</c> response header set to its version.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var order = await _sender.Send(new GetOrder(id), ct);
        Response.SetEtag(order);
        return this.ToOkResponse(order);
    }

    /// <summary>
    /// Replaces a draft order's lines with a new set.
    /// </summary>
    /// <param name="id">Order to edit, from the route.</param>
    /// <param name="body">New requested lines.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the updated order, and an <c>ETag</c> response header set to its new version.</returns>
    [HttpPut("{id:guid}/lines")]
    public async Task<IActionResult> ReplaceLines(Guid id, [FromBody] IReadOnlyCollection<OrderLineInput> body, CancellationToken ct)
    {
        var order = await _sender.Send(new ReplaceOrderLines(id, Request.RequireVersion(), body), ct);
        Response.SetEtag(order);
        return this.ToOkResponse(order);
    }

    /// <summary>
    /// Requests confirmation of a draft order, moving it to PendingInventory.
    /// </summary>
    /// <param name="id">Order to confirm, from the route.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the updated order, and an <c>ETag</c> response header set to its new version.</returns>
    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        var order = await _sender.Send(new ConfirmOrder(id, Request.RequireVersion()), ct);
        Response.SetEtag(order);
        return this.ToOkResponse(order);
    }

    /// <summary>
    /// Cancels an order.
    /// </summary>
    /// <param name="id">Order to cancel, from the route.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the updated order, and an <c>ETag</c> response header set to its new version.</returns>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var order = await _sender.Send(new CancelOrder(id, Request.RequireVersion()), ct);
        Response.SetEtag(order);
        return this.ToOkResponse(order);
    }

    /// <summary>
    /// Undoes the confirmation of an order.
    /// </summary>
    /// <param name="id">Order to undo confirmation for, from the route.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the updated order, and an <c>ETag</c> response header set to its new version.</returns>
    [HttpPost("{id:guid}/undo-confirm")]
    public async Task<IActionResult> UndoConfirm(Guid id, CancellationToken ct)
    {
        var order = await _sender.Send(new UndoConfirmOrder(id, Request.RequireVersion()), ct);
        Response.SetEtag(order);
        return this.ToOkResponse(order);
    }
}
