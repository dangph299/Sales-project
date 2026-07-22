namespace Sales.Application.Features.Customers.DTOs;

/// <summary>
/// The narrow customer projection behind the phone autocomplete.
/// </summary>
/// <remarks>
/// Deliberately not <see cref="CustomerDto"/>: a dropdown needs just enough to tell two customers
/// apart and to fill the form, so versions, audit columns and soft-delete bookkeeping have no
/// business crossing the wire on every keystroke.
/// </remarks>
/// <param name="CustomerId">Customer's unique identifier.</param>
/// <param name="Phone">Customer's phone number as stored.</param>
/// <param name="Name">Customer's name.</param>
/// <param name="Email">Customer's email, or <see langword="null"/>.</param>
/// <param name="Address">Customer's address, or <see langword="null"/>.</param>
/// <param name="Status">Customer's lifecycle status.</param>
public sealed record CustomerLookupDto(
    Guid CustomerId,
    string Phone,
    string Name,
    string? Email,
    string? Address,
    string Status);
