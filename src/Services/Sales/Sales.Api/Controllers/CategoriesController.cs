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
/// HTTP API for category administration.
/// </summary>
[ApiController]
[Authorize]
[Route("api/categories")]
public sealed class CategoriesController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Lists categories available for catalog assignment. Readable by any authenticated user because
    /// the product form binds its category dropdown to this list; mutations remain Admin-only.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the categories ordered by sort order, then name.</returns>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var categories = await sender.Send(new ListCategoriesQuery(), ct);
        return this.ToOkResponse(categories);
    }

    /// <summary>
    /// Creates a category.
    /// </summary>
    /// <param name="request">Category to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>201 Created</c> with the created category.</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequestDto request, CancellationToken ct)
    {
        var category = await sender.Send(
            new CreateCategoryCommand(
                request.CategoryCode,
                request.Name,
                request.Description,
                request.ParentCategoryId,
                request.SortOrder),
            ct);
        return this.ToCreatedResponse("/api/categories", category);
    }

    /// <summary>
    /// Updates a category.
    /// </summary>
    /// <param name="id">Category identifier.</param>
    /// <param name="request">Category changes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the updated category.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequestDto request, CancellationToken ct)
    {
        var category = await sender.Send(
            new UpdateCategoryCommand(
                id,
                request.Name,
                request.Description,
                request.ParentCategoryId,
                request.SortOrder,
                request.Status),
            ct);
        return this.ToOkResponse(category);
    }

    /// <summary>
    /// Soft-deletes a category.
    /// </summary>
    /// <param name="id">Category identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>204 No Content</c> after the category has been soft-deleted.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteCategoryCommand(id), ct);
        return this.ToNoContentResponse();
    }
}
