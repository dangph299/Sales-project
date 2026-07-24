namespace Inventory.Infrastructure;

/// <summary>
/// Stable Redis distributed-lease resource names for Inventory messaging recurring jobs that
/// genuinely require single-instance execution. Jobs that run safely without coordination do not
/// get an entry here.
/// </summary>
public static class InventoryMessagingJobResources
{
    /// <summary>Resource name for Inventory inbound dead-letter replay.</summary>
    public const string ReplayDeadLetter = "jobs:inventory:messaging:replay-dead-letter";
}
