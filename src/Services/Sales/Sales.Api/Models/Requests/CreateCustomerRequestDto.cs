namespace Sales.Api.Models.Requests;

/// <summary>
/// HTTP request body for <c>POST /api/customers</c>.
/// </summary>
public sealed class CreateCustomerRequestDto
{
    /// <summary>
    /// Gets the customer's name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the customer's phone number.
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
