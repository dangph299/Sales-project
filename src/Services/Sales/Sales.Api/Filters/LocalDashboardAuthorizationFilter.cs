using Hangfire.Dashboard;

namespace Sales.Api.Filters;

/// <summary>
/// Restricts access to the Hangfire dashboard to requests originating from the local machine (loopback).
/// </summary>
public sealed class LocalDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    /// <inheritdoc/>
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        return http.Connection.RemoteIpAddress is not null && System.Net.IPAddress.IsLoopback(http.Connection.RemoteIpAddress);
    }
}
