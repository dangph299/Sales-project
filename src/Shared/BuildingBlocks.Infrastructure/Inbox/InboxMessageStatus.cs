namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Processing state for a consumed integration event recorded in a service inbox.
/// </summary>
public enum InboxMessageStatus
{
    /// <summary>The event has been processed successfully.</summary>
    Processed = 0,

    /// <summary>The event failed processing but can still be retried.</summary>
    Failed = 1,

    /// <summary>The event exceeded the configured failed-attempt limit and was isolated.</summary>
    DeadLettered = 2
}
