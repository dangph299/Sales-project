using System.Text.Json.Serialization;

namespace BuildingBlocks.Web.Models;

/// <summary>
/// Standard response envelope for API operations that do not return data.
/// Use it when a controller should return a consistent success or failure shape without a payload.
/// </summary>
public sealed record ApiResponse
{
    private ApiResponse(bool success, string? message, string? correlationId)
    {
        IsSuccess = success;
        Message = message;
        CorrelationId = correlationId;
    }

    /// <summary>
    /// Gets whether the operation completed successfully.
    /// </summary>
    [JsonPropertyName("success")]
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets an optional client-safe response message.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Gets the optional correlation identifier for the request.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Creates a successful response.
    /// </summary>
    /// <param name="message">Optional client-safe response message.</param>
    /// <param name="correlationId">Optional request correlation identifier.</param>
    /// <returns>A successful API response envelope.</returns>
    public static ApiResponse Success(string? message = null, string? correlationId = null)
    {
        return new ApiResponse(true, message, correlationId);
    }

    /// <summary>
    /// Creates a failed response.
    /// </summary>
    /// <param name="message">Optional client-safe response message.</param>
    /// <param name="correlationId">Optional request correlation identifier.</param>
    /// <returns>A failed API response envelope.</returns>
    public static ApiResponse Failure(string? message = null, string? correlationId = null)
    {
        return new ApiResponse(false, message, correlationId);
    }
}
