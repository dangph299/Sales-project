namespace Sales.Application.Features.Orders.DTOs;

/// <summary>
/// The customer details supplied with a create-order request.
/// </summary>
/// <remarks>
/// The caller states who the order is for; it does not state whether that customer already exists.
/// The backend resolves the phone number to an existing customer or creates one, and records these
/// values on the order as its own snapshot either way.
/// </remarks>
/// <param name="Phone">Customer's phone number, in any format containing 9 to 15 digits.</param>
/// <param name="Name">Customer's name as it should appear on this order.</param>
/// <param name="Email">Customer's email, or <see langword="null"/>.</param>
/// <param name="Address">Customer's address, or <see langword="null"/>.</param>
public sealed record CreateOrderCustomer(string Phone, string Name, string? Email = null, string? Address = null);
