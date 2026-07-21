using BuildingBlocks.Web.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sales.Api.Extensions;
using Sales.Api.Models.Requests;
using Sales.Application.Features.Customers.Commands;
using Sales.Application.Features.Customers.Enums;
using Sales.Application.Features.Customers.Queries;

namespace Sales.Api.Controllers;

/// <summary>
/// HTTP API for creating, updating, and searching customers. Dispatches to Application via MediatR.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin,Sales")]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes the controller with the MediatR sender used to dispatch commands and queries.
    /// </summary>
    /// <param name="sender">MediatR sender.</param>
    public CustomersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    /// <param name="request">Customer to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>201 Created</c> with the created customer.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequestDto request, CancellationToken ct)
    {
        var command = new CreateCustomer(request.Name, request.Phone, request.Email, request.Address);
        var customer = await _sender.Send(command, ct);
        return this.ToCreatedResponse("/api/customers", customer);
    }

    /// <summary>
    /// Searches customers by name and/or phone number.
    /// </summary>
    /// <param name="name">An optional substring to match against the customer's name.</param>
    /// <param name="phone">An optional value to match against the customer's phone number.</param>
    /// <param name="phoneMatch">How <paramref name="phone"/> should be matched (prefix or suffix). Defaults to prefix.</param>
    /// <param name="page">1-based page number. Defaults to 1.</param>
    /// <param name="pageSize">Maximum page size. Defaults to 20.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with a page of matching customers.</returns>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? name,
        [FromQuery] string? phone,
        [FromQuery] PhoneMatch phoneMatch = PhoneMatch.Prefix,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var customers = await _sender.Send(new SearchCustomers(name, phone, phoneMatch, page, pageSize), ct);
        return this.ToOkResponse(customers);
    }

    /// <summary>
    /// Loads a single customer by its identifier.
    /// </summary>
    /// <param name="id">Customer identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the customer, and an <c>ETag</c> response header set to its version.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var customer = await _sender.Send(new GetCustomer(id), ct);
        Response.SetEtag(customer);
        return this.ToOkResponse(customer);
    }

    /// <summary>
    /// Updates an existing customer's contact details.
    /// </summary>
    /// <param name="id">Customer to update, from the route.</param>
    /// <param name="body">Customer's new contact details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the updated customer.</returns>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest body, CancellationToken ct)
    {
        var customer = await _sender.Send(new UpdateCustomer(id, body.Name, body.Phone, body.Email, body.Address), ct);
        return this.ToOkResponse(customer);
    }

    /// <summary>
    /// Updates a customer's lifecycle status.
    /// </summary>
    /// <param name="id">Customer identifier.</param>
    /// <param name="body">Status change.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the updated customer.</returns>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateCustomerStatusRequestDto body, CancellationToken ct)
    {
        var customer = await _sender.Send(new UpdateCustomerStatusCommand(id, body.Status), ct);
        return this.ToOkResponse(customer);
    }

    /// <summary>
    /// Soft-deletes an existing customer.
    /// </summary>
    /// <param name="id">Customer identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>204 No Content</c> after the customer has been soft-deleted.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteCustomer(id), ct);
        return this.ToNoContentResponse();
    }
}
