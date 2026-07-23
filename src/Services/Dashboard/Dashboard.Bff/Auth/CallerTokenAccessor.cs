using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Dashboard.Bff.Auth;

/// <summary>
/// Reads the bearer token from the current inbound request's <c>Authorization</c> header via
/// <see cref="IHttpContextAccessor"/>.
/// </summary>
public sealed class CallerTokenAccessor : ICallerTokenAccessor
{
    private const string BearerPrefix = "Bearer ";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CallerTokenAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public bool TryGetToken(out string token)
    {
        var authorizationHeader = _httpContextAccessor.HttpContext?.Request.Headers[HeaderNames.Authorization]
            ?? StringValues.Empty;
        var authorizationValue = authorizationHeader.ToString();

        if (authorizationValue.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var candidate = authorizationValue[BearerPrefix.Length..].Trim();
            if (!string.IsNullOrEmpty(candidate))
            {
                token = candidate;
                return true;
            }
        }

        token = string.Empty;
        return false;
    }
}
