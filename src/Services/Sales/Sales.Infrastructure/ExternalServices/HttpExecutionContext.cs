using System.Security.Claims;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Sales.Application;

namespace Sales.Infrastructure;

/// <summary>
/// <see cref="IExecutionContext"/> implementation backed by the current HTTP request, resolving the
/// actor from the authenticated user and the correlation id from the current trace/request identifier.
/// </summary>
public sealed class HttpExecutionContext(IHttpContextAccessor accessor) : IExecutionContext
{
    /// <inheritdoc/>
    public string Actor => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

    /// <inheritdoc/>
    public Guid CorrelationId
    {
        get
        {
            var traceId = Activity.Current?.TraceId.ToHexString() ?? accessor.HttpContext?.TraceIdentifier;
            return Guid.TryParse(traceId, out var value) ? value : Guid.NewGuid();
        }
    }
}
