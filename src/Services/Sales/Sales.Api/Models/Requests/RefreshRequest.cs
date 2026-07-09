namespace Sales.Api.Models.Requests;

/// <summary>
/// HTTP request body for <c>POST /api/auth/refresh</c>.
/// </summary>
public sealed class RefreshRequest
{
    /// <summary>
    /// Gets the refresh token to exchange for a new access token.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;
}
