namespace Sales.Api.Models.Responses;

/// <summary>
/// HTTP response body returned after successful authentication or token refresh.
/// </summary>
/// <param name="AccessToken">JWT access token used to authorize API requests.</param>
/// <param name="ExpiresIn">Access token lifetime in seconds.</param>
/// <param name="RefreshToken">Refresh token used to request a new access token.</param>
public sealed record TokenResponse(string AccessToken, int ExpiresIn, string RefreshToken);
