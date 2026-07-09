namespace Sales.Application;

/// <summary>
/// Thrown when an optimistic concurrency check fails because a resource's actual version does not
/// match the version a command expected.
/// </summary>
/// <param name="currentVersion">
/// The resource's actual current version.
/// </param>
public sealed class ConflictException(long currentVersion) : Exception("The resource was changed by another request.")
{
    /// <summary>
    /// Gets the resource's actual current version, for callers that want to retry with it.
    /// </summary>
    public long CurrentVersion { get; } = currentVersion;
}
