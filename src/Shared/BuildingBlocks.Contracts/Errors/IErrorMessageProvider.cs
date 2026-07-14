namespace BuildingBlocks.Contracts;

/// <summary>
/// Provides service-specific error descriptions without redefining error codes.
/// </summary>
public interface IErrorMessageProvider
{
    /// <summary>
    /// Returns the description to expose for an error code.
    /// </summary>
    /// <param name="code">Stable error code.</param>
    /// <param name="defaultDescription">Default catalog description.</param>
    /// <returns>Description to expose to clients.</returns>
    string GetDescription(string code, string defaultDescription);
}

/// <summary>
/// Default provider that exposes catalog descriptions unchanged.
/// </summary>
public class DefaultErrorMessageProvider : IErrorMessageProvider
{
    /// <inheritdoc />
    public virtual string GetDescription(string code, string defaultDescription)
    {
        return defaultDescription;
    }
}
