namespace Dashboard.Bff.Options;

/// <summary>
/// Credentials the Dashboard BFF uses to authenticate against downstream services on behalf of
/// dashboard requests.
/// </summary>
public sealed class ServiceAccountOptions
{
    public const string SectionName = "ServiceAccount";

    /// <summary>Service account user name presented to downstream services.</summary>
    public string UserName { get; set; } = "";

    /// <summary>Service account password presented to downstream services.</summary>
    public string Password { get; set; } = "";

    /// <summary>
    /// When <see langword="true"/>, allows blank credentials in the Development environment
    /// (e.g. relying on downstream dev-only anonymous/admin access). Has no effect outside
    /// Development.
    /// </summary>
    public bool AllowAdminDevFallback { get; set; } = false;
}
