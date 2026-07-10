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
/// <param name="UpdatedAt">
/// The UTC instant the customer was last changed.
/// </param>
/// <param name="IsDelete">
/// Whether the customer has been soft-deleted.
/// </param>
/// <param name="DeleteByUser">
/// The user that soft-deleted the customer, or <see langword="null"/> if it is active.
/// </param>
/// <param name="DeletedAt">
/// The UTC instant the customer was soft-deleted, or <see langword="null"/> if it is active.
/// </param>
public sealed record CustomerDto(Guid Id, string Name, string Phone, long Version,
    DateTimeOffset UpdatedAt, bool IsDelete, string? DeleteByUser, DateTimeOffset? DeletedAt);
