namespace Sales.Api.Models.Requests;

/// <summary>
/// Request body for replacing the customer details recorded on an order.
/// </summary>
/// <param name="Name">Customer's name to record on the order.</param>
/// <param name="Phone">Customer's phone number to record on the order, in any format containing 9 to 15 digits.</param>
/// <param name="Email">Customer's email to record on the order, or <see langword="null"/>.</param>
/// <param name="Address">Customer's address to record on the order, or <see langword="null"/>.</param>
public sealed record UpdateOrderCustomerRequest(string Name, string Phone, string? Email, string? Address);
