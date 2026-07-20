using BuildingBlocks.Web.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sales.Api.Extensions;
using Sales.Api.Models.Requests;
using Sales.Application.Features.Products.Commands;
using Sales.Application.Features.Products.Queries;

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
    /// <param name="sender">MediatR sender.</param>
    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="command">Product to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>201 Created</c> with the created product.</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command, CancellationToken ct)
    {
        var product = await _sender.Send(command, ct);
        return this.ToCreatedResponse("/api/products", product);
    }

    /// <summary>
    /// Searches products by name.
    /// </summary>
    /// <param name="productCode">Optional product code filter.</param>
    /// <param name="name">An optional substring to match against the product's name.</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <param name="sku">Optional variant SKU filter.</param>
    /// <param name="colorId">Optional color filter.</param>
    /// <param name="sizeId">Optional size filter.</param>
    /// <param name="status">Optional product status filter.</param>
    /// <param name="page">1-based page number. Defaults to 1.</param>
    /// <param name="pageSize">Maximum page size. Defaults to 20.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with a page of matching products.</returns>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? productCode,
        [FromQuery] string? name,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? sku,
        [FromQuery] Guid? colorId,
        [FromQuery] Guid? sizeId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var products = await _sender.Send(new SearchProductsQuery(productCode, name, categoryId, sku, colorId, sizeId, status, page, pageSize), ct);
        return this.ToOkResponse(products);
    }

    /// <summary>
    /// Loads a single product by its identifier.
    /// </summary>
    /// <param name="id">Product identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the product, and an <c>ETag</c> response header set to its version.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var product = await _sender.Send(new GetProductQuery(id), ct);
        Response.SetEtag(product);
        return this.ToOkResponse(product);
    }

    /// <summary>
    /// Updates an existing product's shared details and lifecycle status.
    /// </summary>
    /// <param name="id">Product to update, from the route.</param>
    /// <param name="body">Product's new values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the updated product.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequestDto body, CancellationToken ct)
    {
        var product = await _sender.Send(new UpdateProductCommand(id, body.Name, body.Description, body.CategoryId, body.Status), ct);
        return this.ToOkResponse(product);
    }

    /// <summary>
    /// Adds a product variant.
    /// </summary>
    /// <param name="id">Product identifier.</param>
    /// <param name="body">Variant to add.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the updated product.</returns>
    [HttpPost("{id:guid}/variants")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddVariant(Guid id, [FromBody] ProductVariantRequestDto body, CancellationToken ct)
    {
        var product = await _sender.Send(new AddProductVariantCommand(id, body.ColorId, body.SizeId, body.Price, body.Status), ct);
        return this.ToOkResponse(product);
    }

    /// <summary>
    /// Updates a product variant.
    /// </summary>
    /// <param name="id">Product identifier.</param>
    /// <param name="variantId">Variant identifier.</param>
    /// <param name="body">Variant changes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the updated product.</returns>
    [HttpPut("{id:guid}/variants/{variantId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateVariant(Guid id, Guid variantId, [FromBody] ProductVariantRequestDto body, CancellationToken ct)
    {
        var product = await _sender.Send(new UpdateProductVariantCommand(id, variantId, body.ColorId, body.SizeId, body.Price, body.Status), ct);
        return this.ToOkResponse(product);
    }

    /// <summary>
    /// Deactivates a product variant.
    /// </summary>
    /// <param name="id">Product identifier.</param>
    /// <param name="variantId">Variant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the updated product.</returns>
    [HttpPost("{id:guid}/variants/{variantId:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateVariant(Guid id, Guid variantId, CancellationToken ct)
    {
        var product = await _sender.Send(new DeactivateProductVariantCommand(id, variantId), ct);
        return this.ToOkResponse(product);
    }

    /// <summary>
    /// Soft-deletes an existing product.
    /// </summary>
    /// <param name="id">Product identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>204 No Content</c> after the product has been soft-deleted.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteProductCommand(id), ct);
        return this.ToNoContentResponse();
    }
}
