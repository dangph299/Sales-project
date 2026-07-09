using MediatR;
using Microsoft.Extensions.Logging;
using Sales.Application;
using Sales.Domain;

namespace Sales.Application.Tests;

public sealed class ErrorLoggingBehaviorTests
{
    private sealed record Ping : IRequest<string>;

    [Fact]
    public async Task Business_rule_violations_log_at_warning_not_error()
    {
        var logger = new RecordingLogger<ErrorLoggingBehavior<Ping, string>>();
        var behavior = new ErrorLoggingBehavior<Ping, string>(logger);

        await Assert.ThrowsAsync<NotFoundException>(() => behavior.Handle(
            new Ping(), _ => throw new NotFoundException("Order", Guid.NewGuid()), CancellationToken.None));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.IsType<NotFoundException>(entry.Exception);
    }

    [Fact]
    public async Task Unexpected_exceptions_log_at_error()
    {
        var logger = new RecordingLogger<ErrorLoggingBehavior<Ping, string>>();
        var behavior = new ErrorLoggingBehavior<Ping, string>(logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(
            new Ping(), _ => throw new InvalidOperationException("boom"), CancellationToken.None));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.IsType<InvalidOperationException>(entry.Exception);
    }

    [Fact]
    public async Task Success_produces_no_log_and_never_swallows_the_result()
    {
        var logger = new RecordingLogger<ErrorLoggingBehavior<Ping, string>>();
        var behavior = new ErrorLoggingBehavior<Ping, string>(logger);

        var result = await behavior.Handle(new Ping(), _ => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task Optimistic_concurrency_conflicts_log_at_warning_not_error()
    {
        // ErrorLoggingBehavior can't reference Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException
        // directly (Application must not depend on Infrastructure), so it matches by type full name -
        // this fake, declared under the same namespace, proves that match actually fires.
        var logger = new RecordingLogger<ErrorLoggingBehavior<Ping, string>>();
        var behavior = new ErrorLoggingBehavior<Ping, string>(logger);

        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>(() => behavior.Handle(
            new Ping(), _ => throw new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException(), CancellationToken.None));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
    }

    [Fact]
    public async Task Failure_log_includes_the_request_for_correlation()
    {
        var logger = new RecordingLogger<ErrorLoggingBehavior<Ping, string>>();
        var behavior = new ErrorLoggingBehavior<Ping, string>(logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(
            new Ping(), _ => throw new InvalidOperationException("boom"), CancellationToken.None));

        var entry = Assert.Single(logger.Entries);
        Assert.Contains("Ping", entry.Message);
    }
}
