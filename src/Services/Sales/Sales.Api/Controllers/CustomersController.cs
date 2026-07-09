using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sales.Api.Extensions;
using Sales.Api.Models.Requests;
using Sales.Application;

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
    /// <param name="sender">
    /// The MediatR sender.
    /// </param>
    public CustomersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    /// <param name="command">
    /// The customer to create.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// <c>201 Created</c> with the created customer.
    /// </returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomer command, CancellationToken ct)
    {
        var customer = await _sender.Send(command, ct);
        return Created("/api/customers", customer);
    }

    /// <summary>
    /// Searches customers by name and/or phone number.
    /// </summary>
    /// <param name="name">
    /// An optional substring to match against the customer's name.
    /// </param>
    /// <param name="phone">
    /// An optional value to match against the customer's phone number.
    /// </param>
    /// <param name="phoneMatch">
    /// How <paramref name="phone"/> should be matched (prefix or suffix). Defaults to prefix.
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
    /// <c>200 OK</c> with a page of matching customers.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? name,
        [FromQuery] string? phone,
        [FromQuery] PhoneMatch phoneMatch = PhoneMatch.Prefix,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        return Ok(await _sender.Send(new SearchCustomers(name, phone, phoneMatch, page, pageSize), ct));
    }

    /// <summary>
    /// Loads a single customer by its identifier.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the customer to load.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// <c>200 OK</c> with the customer, and an <c>ETag</c> response header set to its version.
    /// </returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var customer = await _sender.Send(new GetCustomer(id), ct);
        Response.SetEtag(customer);
        return Ok(customer);
    }

    /// <summary>
    /// Updates an existing customer's name and phone number.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the customer to update, from the route.
    /// </param>
    /// <param name="body">
    /// The customer's new name and phone number.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// <c>200 OK</c> with the updated customer.
    /// </returns>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest body, CancellationToken ct)
    {
        return Ok(await _sender.Send(new UpdateCustomer(id, body.Name, body.Phone), ct));
    }
}
