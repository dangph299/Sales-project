namespace Sales.Api.Models.Requests;

/// <summary>
/// HTTP request body for <c>POST /api/auth/login</c>.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// Gets the username to authenticate.
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the password to authenticate.
    /// </summary>
    public string Password { get; init; } = string.Empty;
}
