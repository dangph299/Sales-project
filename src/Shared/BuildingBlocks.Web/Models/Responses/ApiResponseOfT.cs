using System.Text.Json.Serialization;

namespace BuildingBlocks.Web.Models;

/// <summary>
/// Standard response envelope for API operations that return data.
/// Use it when a controller should return a consistent success or failure shape with a payload.
/// </summary>
public sealed record ApiResponse<T>
{
    private ApiResponse(bool success, T? data, string? message, string? correlationId)
    {
        IsSuccess = success;
        Data = data;
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
    /// Gets the operation payload.
    /// </summary>
    public T? Data { get; }

    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    /// <param name="data">Payload returned by the API operation.</param>
    /// <param name="message">Optional client-safe response message.</param>
    /// <param name="correlationId">Optional request correlation identifier.</param>
    /// <returns>A successful API response envelope.</returns>
    public static ApiResponse<T> Success(T data, string? message = null, string? correlationId = null)
    {
        return new ApiResponse<T>(true, data, message, correlationId);
    }

    /// <summary>
    /// Creates a failed response with no data.
    /// </summary>
    /// <param name="message">Optional client-safe response message.</param>
    /// <param name="correlationId">Optional request correlation identifier.</param>
    /// <returns>A failed API response envelope.</returns>
    public static ApiResponse<T> Failure(string? message = null, string? correlationId = null)
    {
        return new ApiResponse<T>(false, default, message, correlationId);
    }
}
