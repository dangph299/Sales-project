namespace Sales.Application;

/// <summary>
/// Read model for a customer, returned by queries and API responses.
/// </summary>
/// <param name="Id">
/// The customer's unique identifier.
/// </param>
/// <param name="Name">
/// The customer's name.
/// </param>
/// <param name="Phone">
/// The customer's normalized phone number.
/// </param>
/// <param name="Version">
/// The customer's current optimistic concurrency version.
/// </param>
public sealed record CustomerDto(Guid Id, string Name, string Phone, long Version);
