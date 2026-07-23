using System.Text.Json.Serialization;

namespace Dashboard.Bff.Clients.Payloads;

/// <summary>
/// Minimal shape of the shared <c>ApiResponse&lt;T&gt;</c> envelope every downstream endpoint wraps
/// its payload in. Deserializable (unlike <c>BuildingBlocks.Web.Models.ApiResponse&lt;T&gt;</c>,
/// which has no public setters), mirroring the pattern in <c>ServiceAccountTokenProvider</c>.
/// </summary>
/// <typeparam name="TData">Shape of the wrapped payload.</typeparam>
public sealed record ApiEnvelope<TData>(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("correlationId")] string? CorrelationId,
    [property: JsonPropertyName("data")] TData? Data);
