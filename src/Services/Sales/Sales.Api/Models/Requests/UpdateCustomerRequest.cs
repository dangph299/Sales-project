namespace Sales.Api.Models.Requests;

/// <summary>
/// HTTP request body for <c>PUT /api/customers/{id}</c>. Intentionally excludes <c>Id</c>, which
/// comes from the route.
/// </summary>
public sealed class UpdateCustomerRequest
{
    /// <summary>
    /// Gets the customer's new name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the customer's new phone number.
    /// </summary>
    public string Phone { get; init; } = string.Empty;

    /// <summary>
    /// Gets the customer's email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets the customer's address.
    /// </summary>
    public string? Address { get; init; }
}
