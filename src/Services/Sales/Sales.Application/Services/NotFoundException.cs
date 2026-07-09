namespace Sales.Application;

/// <summary>
/// Thrown when a requested resource does not exist.
/// </summary>
/// <param name="resource">
/// The kind of resource that was not found, for example <c>"Customer"</c>.
/// </param>
/// <param name="key">
/// The identifier that was looked up.
/// </param>
public sealed class NotFoundException(string resource, object key) : Exception($"{resource} '{key}' was not found.");
