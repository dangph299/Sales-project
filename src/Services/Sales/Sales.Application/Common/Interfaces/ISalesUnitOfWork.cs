namespace Sales.Application.Common.Interfaces;

/// <summary>
/// The Sales unit of work, adding the ability to abandon everything staged so far.
/// </summary>
/// <remarks>
/// Needed only by a handler that retries after a failed save: the aggregates it staged are still
/// pending, and saving again would re-attempt the same rejected write. Handlers that never retry
/// should depend on the narrower <see cref="IUnitOfWork"/>.
/// </remarks>
public interface ISalesUnitOfWork : IUnitOfWork
{
    /// <summary>
    /// Abandons every change staged since the last save, so the caller can rebuild its work from
    /// freshly read state.
    /// </summary>
    void DiscardPendingChanges();
}
