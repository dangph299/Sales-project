namespace Dashboard.Bff.Auth;

/// <summary>
/// Provides an access token for the Dashboard BFF's service account, transparently logging in and
/// caching the token as needed.
/// </summary>
public interface IServiceTokenProvider
{
    /// <summary>
    /// Returns a cached, unexpired access token, logging in again if there is none or it has
    /// expired.
    /// </summary>
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
}
