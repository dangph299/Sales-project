namespace Sales.Application;

/// <summary>
/// Thrown when a requested resource does not exist.
/// </summary>
/// <param name="resource">Kind of resource that was not found, for example <c>"Customer"</c>.</param>
/// <param name="key">Identifier that was looked up.</param>
public sealed class NotFoundException(string resource, object key) : Exception($"{resource} '{key}' was not found.");
