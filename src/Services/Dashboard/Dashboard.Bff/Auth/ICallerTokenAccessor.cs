namespace Dashboard.Bff.Auth;

/// <summary>
/// Reads the bearer token from the current inbound request, when there is one.
/// </summary>
public interface ICallerTokenAccessor
{
    /// <summary>
    /// Attempts to read the bearer token from the current HTTP context's
    /// <c>Authorization</c> header.
    /// </summary>
    /// <param name="token">The bearer token, or an empty string if none was found.</param>
    /// <returns><see langword="true"/> if a bearer token was found; otherwise <see langword="false"/>.</returns>
    bool TryGetToken(out string token);
}
