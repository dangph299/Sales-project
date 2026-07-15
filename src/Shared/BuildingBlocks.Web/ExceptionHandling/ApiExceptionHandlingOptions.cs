using BuildingBlocks.Contracts;

namespace BuildingBlocks.Web.ExceptionHandling;

/// <summary>
/// Configures service-specific exception mappings for the shared API exception handler.
/// </summary>
public sealed class ApiExceptionHandlingOptions
{
    private readonly List<Func<Exception, IErrorCatalog, ApiExceptionMapping?>> mappings = [];

    internal ApiExceptionMapping? TryMap(Exception exception, IErrorCatalog errorCatalog)
    {
        foreach (var mapping in mappings)
        {
            var result = mapping(exception, errorCatalog);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Maps a service-specific exception to a client-safe API response.
    /// </summary>
    /// <param name="mapping">Mapping function for the exception type.</param>
    /// <typeparam name="TException">Exception type handled by this mapping.</typeparam>
    public void Map<TException>(Func<TException, IErrorCatalog, ApiExceptionMapping> mapping)
        where TException : Exception
    {
        mappings.Add((exception, errorCatalog) =>
        {
            if (exception is not TException typedException)
            {
                return null;
            }

            return mapping(typedException, errorCatalog);
        });
    }
}
