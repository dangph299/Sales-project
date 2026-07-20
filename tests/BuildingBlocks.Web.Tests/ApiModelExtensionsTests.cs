using System.Diagnostics;
using BuildingBlocks.Web.Extensions;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Web.Tests;

/// <summary>
/// Guards the single definition of TraceId and CorrelationId that responses, the request summary,
/// and the exception handler all resolve through. A second definition drifting away from this one
/// is what made a client-reported id unfindable in Seq.
/// </summary>
public sealed class ApiModelExtensionsTests
{
    [Fact]
    public void Trace_id_is_the_w3c_trace_id_when_an_activity_is_running()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "0HN7ABC:00000003" };
        using var activity = new Activity("request").Start();

        var traceId = context.GetTraceId();

        Assert.Equal(activity.TraceId.ToHexString(), traceId);
        Assert.NotEqual(context.TraceIdentifier, traceId);
    }

    [Fact]
    public void Trace_id_falls_back_to_the_request_id_without_an_activity()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "0HN7ABC:00000003" };
        Activity.Current = null;

        Assert.Equal("0HN7ABC:00000003", context.GetTraceId());
    }

    [Fact]
    public void Correlation_id_prefers_the_caller_supplied_header()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = "order-import-42";
        using var activity = new Activity("request").Start();

        Assert.Equal("order-import-42", context.GetCorrelationId());
    }

    [Fact]
    public void Correlation_id_falls_back_to_the_trace_id_rather_than_null()
    {
        var context = new DefaultHttpContext();
        using var activity = new Activity("request").Start();

        Assert.Equal(activity.TraceId.ToHexString(), context.GetCorrelationId());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Correlation_id_ignores_a_blank_header(string header)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = header;
        using var activity = new Activity("request").Start();

        Assert.Equal(activity.TraceId.ToHexString(), context.GetCorrelationId());
    }
}
