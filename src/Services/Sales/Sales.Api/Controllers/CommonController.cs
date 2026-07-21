using BuildingBlocks.Web.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sales.Application.Features.Products.Queries;

namespace Sales.Api.Controllers;

/// <summary>
/// Read-only HTTP API exposing seeded reference data. Clients bind dropdowns to these lists rather
/// than hardcoding seeded identifiers: each item carries both the stable business <c>code</c> and the
/// persistence <c>id</c> that write requests must submit.
/// </summary>
[ApiController]
[Authorize]
[Route("api/common")]
public sealed class CommonController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Lists every color available to product variants.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the colors ordered by code.</returns>
    [HttpGet("colors")]
    public async Task<IActionResult> ListColors(CancellationToken ct)
    {
        var colors = await sender.Send(new ListColorsQuery(), ct);
        return this.ToOkResponse(colors);
    }

    /// <summary>
    /// Lists every size available to product variants.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the sizes ordered by sort order.</returns>
    [HttpGet("sizes")]
    public async Task<IActionResult> ListSizes(CancellationToken ct)
    {
        var sizes = await sender.Send(new ListSizesQuery(), ct);
        return this.ToOkResponse(sizes);
    }
}
