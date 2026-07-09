namespace BuildingBlocks.Contracts;

/// <summary>
/// Version suffixes used in Kafka topic names, so a breaking contract change can ship as a new
/// topic (<c>.v2</c>) instead of mutating an existing one's payload shape.
/// </summary>
public static class ContractVersions
{
    /// <summary>The current (and so far only) contract version.</summary>
    public const string V1 = "v1";
}
