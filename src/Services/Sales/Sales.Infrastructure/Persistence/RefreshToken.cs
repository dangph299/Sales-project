namespace Sales.Infrastructure;

/// <summary>
/// Persistence record for an issued JWT refresh token, used by <c>AuthController</c> to validate
/// and revoke refresh tokens outside of the CQRS pipeline.
/// </summary>
public sealed class RefreshToken
{
    /// <summary>
    /// Gets or sets the unique identifier of this refresh token record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the user this token was issued to.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the hash of the refresh token value (the raw token is never persisted).
    /// </summary>
    public string TokenHash { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UTC instant after which this token is no longer valid.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC instant this token was issued.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC instant this token was revoked, or <see langword="null"/> if it has not been revoked.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// Gets or sets the refresh token that replaced this token during rotation.
    /// </summary>
    public Guid? ReplacedByTokenId { get; set; }

    /// <summary>
    /// Gets or sets the IP address that requested this token, when available.
    /// </summary>
    public string? CreatedByIp { get; set; }
}
