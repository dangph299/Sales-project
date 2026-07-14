using Inventory.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Tests;

public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task Concurrency_exception_maps_to_409()
    {
        var problemDetails = new FakeProblemDetailsService();
        var middleware = new ExceptionHandlingMiddleware(problemDetails);
        var context = new DefaultHttpContext();

        var handled = await middleware.TryHandleAsync(context, new DbUpdateConcurrencyException(), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal(409, problemDetails.LastDetails!.Status);
    }

    [Fact]
    public async Task Non_concurrency_data_integrity_exception_maps_to_500()
    {
        var problemDetails = new FakeProblemDetailsService();
        var middleware = new ExceptionHandlingMiddleware(problemDetails);
        var context = new DefaultHttpContext();

        var handled = await middleware.TryHandleAsync(context, new DbUpdateException("FK violation"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal(500, problemDetails.LastDetails!.Status);
    }

    private sealed class FakeProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetails? LastDetails { get; private set; }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            LastDetails = context.ProblemDetails;
            return ValueTask.FromResult(true);
        }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            LastDetails = context.ProblemDetails;
            return ValueTask.CompletedTask;
        }
    }
}
