using MediatR;
using Microsoft.Extensions.Logging;

namespace Sales.Application.Tests;

public sealed class BuildingBlocksApplicationBehaviorTests
{
    [Fact]
    public async Task Performance_behavior_returns_inner_response()
    {
        var logger = new RecordingLogger<PerformanceBehavior<QueryPing, string>>();
        var behavior = new PerformanceBehavior<QueryPing, string>(logger, TimeSpan.FromDays(1));

        var response = await behavior.Handle(new QueryPing(), _ => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", response);
        Assert.DoesNotContain(logger.Entries, x => x.Level >= LogLevel.Warning);
    }

    private sealed record QueryPing : IRequest<string>;
}
