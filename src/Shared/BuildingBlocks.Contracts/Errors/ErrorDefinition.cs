namespace BuildingBlocks.Contracts;

/// <summary>
/// Public error contract definition.
/// </summary>
/// <param name="Code">Stable machine-readable error code.</param>
/// <param name="Description">Human-readable default description.</param>
public sealed record ErrorDefinition(string Code, string Description);
