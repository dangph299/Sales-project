using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sales.Api.Extensions;
using Sales.Api.Models.Requests;
using Sales.Application;

namespace Sales.Api.Controllers;

/// <summary>
/// HTTP API for creating, updating, and searching catalog products. Dispatches to Application via
/// MediatR. Create/Update are restricted to the Admin role.
/// </summary>
[ApiController]
[Authorize]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes the controller with the MediatR sender used to dispatch commands and queries.
    /// </summary>
    /// <param name="sender">
    /// The MediatR sender.
    /// </param>
    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="command">
    /// The product to create.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// <c>201 Created</c> with the created product.
    /// </returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProduct command, CancellationToken ct)
    {
        var product = await _sender.Send(command, ct);
        return Created("/api/products", product);
    }

    /// <summary>
    /// Searches products by name.
    /// </summary>
    /// <param name="name">
    /// An optional substring to match against the product's name.
    /// </param>
    /// <param name="page">
    /// The 1-based page number to return. Defaults to 1.
    /// </param>
    /// <param name="pageSize">
    /// The maximum number of items per page. Defaults to 20.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// <c>200 OK</c> with a page of matching products.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? name, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        return Ok(await _sender.Send(new SearchProducts(name, page, pageSize), ct));
    }

    /// <summary>
    /// Loads a single product by its identifier.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the product to load.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// <c>200 OK</c> with the product, and an <c>ETag</c> response header set to its version.
    /// </returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var product = await _sender.Send(new GetProduct(id), ct);
        Response.SetEtag(product);
        return Ok(product);
    }

    /// <summary>
    /// Updates an existing product's name, price, and active flag.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the product to update, from the route.
    /// </param>
    /// <param name="body">
    /// The product's new values.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// <c>200 OK</c> with the updated product.
    /// </returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest body, CancellationToken ct)
    {
        return Ok(await _sender.Send(new UpdateProduct(id, body.Name, body.Price, body.IsActive), ct));
    }

    /// <summary>
    /// Soft-deletes an existing product.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the product to delete.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// <c>204 No Content</c> after the product has been soft-deleted.
    /// </returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteProduct(id), ct);
        return NoContent();
    }
}
