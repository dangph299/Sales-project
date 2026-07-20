using BuildingBlocks.Web.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sales.Api.Extensions;
using Sales.Api.Models.Requests;
using Sales.Application.Features.Products.Commands;

namespace Sales.Api.Controllers;

/// <summary>
/// HTTP API for category administration.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/categories")]
public sealed class CategoriesController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Creates a category.
    /// </summary>
    /// <param name="request">Category to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>201 Created</c> with the created category.</returns>
    [HttpPost]
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
}
