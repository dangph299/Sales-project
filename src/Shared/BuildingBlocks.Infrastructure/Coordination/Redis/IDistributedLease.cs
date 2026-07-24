namespace BuildingBlocks.Infrastructure.Coordination.Redis;

/// <summary>
/// A held lease over a named resource. The lease is only valid until it is disposed or its
/// underlying TTL expires, whichever comes first.
/// </summary>
public interface IDistributedLease : IAsyncDisposable
{
    /// <summary>The resource name passed to <see cref="IDistributedLeaseManager.TryAcquireAsync"/>.</summary>
    string Resource { get; }

    /// <summary>The random token that identifies this acquisition as the current owner.</summary>
    string OwnerToken { get; }

    /// <summary>
    /// <see langword="true"/> until this lease has been released via <see cref="IAsyncDisposable.DisposeAsync"/>.
    /// Does not reflect whether the underlying TTL has since expired.
    /// </summary>
    bool IsHeld { get; }
}
