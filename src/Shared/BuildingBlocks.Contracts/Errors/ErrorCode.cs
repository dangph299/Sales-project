namespace BuildingBlocks.Contracts;

/// <summary>
/// Stable public error code value.
/// </summary>
/// <param name="Value">Machine-readable error code.</param>
public readonly record struct ErrorCode(string Value)
{
    /// <summary>
    /// Returns the underlying string value.
    /// </summary>
    /// <returns>Machine-readable error code.</returns>
    public override string ToString() => Value;
}
