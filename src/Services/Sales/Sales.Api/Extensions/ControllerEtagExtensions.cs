using Sales.Application;

namespace Sales.Api.Extensions;

/// <summary>
/// HTTP request/response helpers implementing optimistic-concurrency ETags on top of DTO version numbers.
/// </summary>
internal static class ControllerEtagExtensions
{
    /// <summary>
    /// Sets the response's <c>ETag</c> header to the version of a known DTO type.
    /// </summary>
    /// <param name="response">
    /// The response to set the header on.
    /// </param>
    /// <param name="dto">
    /// The DTO whose version should be reflected as the ETag. Must be a <see cref="ProductDto"/>,
    /// <see cref="CustomerDto"/>, or <see cref="OrderDto"/>; other types get ETag <c>"0"</c>.
    /// </param>
    public static void SetEtag(this HttpResponse response, object dto)
    {
        var version = dto switch
        {
            ProductDto product => product.Version,
            CustomerDto customer => customer.Version,
            OrderDto order => order.Version,
            _ => 0
        };
        response.Headers.ETag = $"\"{version}\"";
    }

    /// <summary>
    /// Reads and parses the request's <c>If-Match</c> header as the expected aggregate version,
    /// required by mutating endpoints for optimistic concurrency.
    /// </summary>
    /// <param name="request">
    /// The request to read the header from.
    /// </param>
    /// <returns>
    /// The expected version parsed from the <c>If-Match</c> header.
    /// </returns>
    /// <exception cref="BadHttpRequestException">
    /// Thrown with status 428 when the <c>If-Match</c> header is missing or not a numeric ETag.
    /// </exception>
    public static long RequireVersion(this HttpRequest request)
    {
        var value = request.Headers.IfMatch.ToString().Trim('"');
        if (!long.TryParse(value, out var version))
        {
            throw new BadHttpRequestException("A numeric If-Match ETag is required.", 428);
        }

        return version;
    }
}
