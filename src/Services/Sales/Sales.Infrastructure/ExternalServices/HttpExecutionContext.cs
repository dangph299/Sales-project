using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Sales.Application.Common.Interfaces;

namespace Sales.Infrastructure;

/// <summary>
/// Execution context for the current HTTP request.
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
