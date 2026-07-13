using Microsoft.AspNetCore.Http;
using Serilog.AspNetCore;
using Serilog.Events;

namespace BuildingBlocks.Web;

/// <summary>
/// Shared <c>UseSerilogRequestLogging</c> level customization used by every service instead of a
/// per-service copy.
/// </summary>
public static class RequestLoggingDefaults
{
    private static readonly string[] QuietPathPrefixes = ["/health", "/hangfire"];

    /// <summary>
    /// Health checks and dashboard polling are not incidents - keep them out of the
    /// Information-level HTTP summary log so on-call signal isn't drowned by uptime noise.
    /// </summary>
    /// <param name="options">Request logging options to configure.</param>
    public static void Configure(RequestLoggingOptions options)
    {
        options.GetLevel = (context, _, exception) =>
        {
            if (exception is not null || context.Response.StatusCode > 499) return LogEventLevel.Error;
            return IsQuietPath(context.Request.Path) ? LogEventLevel.Debug : LogEventLevel.Information;
        };
    }

    private static bool IsQuietPath(PathString path) =>
        QuietPathPrefixes.Any(prefix => path.StartsWithSegments(prefix));
}
