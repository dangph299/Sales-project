namespace Dashboard.Bff.Clients.Payloads;

/// <summary>
/// Narrow shape of the shared <c>PagedResult&lt;T&gt;</c> response body for paged list endpoints.
/// </summary>
/// <typeparam name="TItem">Shape of each list item.</typeparam>
public sealed record PagedResultPayload<TItem>(
    IReadOnlyList<TItem> Items,
    int Page,
    int PageSize,
    long Total);
