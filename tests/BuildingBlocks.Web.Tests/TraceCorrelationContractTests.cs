using System.Diagnostics;
using BuildingBlocks.Web.Extensions;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace BuildingBlocks.Web.Tests;

/// <summary>
/// Closes the loop the whole TraceId design rests on: the id handed to an API client and the id
/// Serilog stamps on log events must be the same string, or a client-reported TraceId finds nothing
/// in Seq. Each half is asserted against the same running <see cref="Activity"/>.
/// </summary>
public sealed class TraceCorrelationContractTests
{
    [Fact]
    public void The_trace_id_returned_to_clients_is_the_trace_id_serilog_stamps_on_log_events()
    {
        LogEvent? captured = null;
        using var logger = new LoggerConfiguration()
            .WriteTo.Sink(new DelegatingSink(e => captured = e))
            .CreateLogger();

        var context = new DefaultHttpContext { TraceIdentifier = "0HN7ABC:00000003" };
        using var activity = new Activity("request").Start();

        var returnedToClient = context.GetTraceId();
        logger.Error(new InvalidOperationException("boom"), "Request failed");

        Assert.NotNull(captured);
        Assert.NotNull(captured!.TraceId);
        Assert.Equal(captured.TraceId!.Value.ToHexString(), returnedToClient);
    }

    private sealed class DelegatingSink(Action<LogEvent> write) : ILogEventSink
    {
        public void Emit(LogEvent logEvent) => write(logEvent);
    }
}
