using BuildingBlocks.Contracts;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sales.Api.Middleware;
using Sales.Application;

namespace Sales.Api.Tests;

public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task Concurrency_exception_maps_to_409_with_shared_code()
    {
        var problemDetails = new FakeProblemDetailsService();
        var middleware = CreateMiddleware(problemDetails);
        var context = new DefaultHttpContext();

        var handled = await middleware.TryHandleAsync(context, new DbUpdateConcurrencyException(), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.ConcurrencyConflict, problemDetails.LastDetails!.Extensions["code"]);
        Assert.Equal("The sales resource was changed by another request.", problemDetails.LastDetails.Extensions["description"]);
    }

    [Fact]
    public async Task Not_found_exception_maps_to_404_with_shared_code()
    {
        var problemDetails = new FakeProblemDetailsService();
        var middleware = CreateMiddleware(problemDetails);
        var context = new DefaultHttpContext();

        var handled = await middleware.TryHandleAsync(context, new NotFoundException("Order", Guid.NewGuid()), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(404, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.NotFound, problemDetails.LastDetails!.Extensions["code"]);
        Assert.Null(problemDetails.LastDetails.Detail);
    }

    [Fact]
    public async Task Validation_exception_maps_to_400_with_shared_code()
    {
        var problemDetails = new FakeProblemDetailsService();
        var middleware = CreateMiddleware(problemDetails);
        var context = new DefaultHttpContext();
        var exception = new ValidationException([new ValidationFailure("Name", "Name is required.")]);

        var handled = await middleware.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.Validation, problemDetails.LastDetails!.Extensions["code"]);
        Assert.True(problemDetails.LastDetails.Extensions.ContainsKey("errors"));
    }

    [Fact]
    public async Task Unique_violation_maps_to_409_with_shared_code()
    {
        var problemDetails = new FakeProblemDetailsService();
        var middleware = CreateMiddleware(problemDetails);
        var context = new DefaultHttpContext();
        var unique = new PostgresException("duplicate key", "ERROR", "ERROR", "23505");

        var handled = await middleware.TryHandleAsync(context, new DbUpdateException("save failed", unique), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.UniqueViolation, problemDetails.LastDetails!.Extensions["code"]);
    }

    [Fact]
    public async Task Unknown_DbUpdateException_maps_to_500()
    {
        var problemDetails = new FakeProblemDetailsService();
        var middleware = CreateMiddleware(problemDetails);
        var context = new DefaultHttpContext();

        var handled = await middleware.TryHandleAsync(context, new DbUpdateException("FK violation"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.InternalServerError, problemDetails.LastDetails!.Extensions["code"]);
        Assert.Null(problemDetails.LastDetails.Detail);
    }

    [Fact]
    public async Task Unknown_exception_maps_to_500_without_internal_detail()
    {
        var problemDetails = new FakeProblemDetailsService();
        var middleware = CreateMiddleware(problemDetails);
        var context = new DefaultHttpContext();

        var handled = await middleware.TryHandleAsync(context, new InvalidOperationException("secret internals"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.InternalServerError, problemDetails.LastDetails!.Extensions["code"]);
        Assert.Null(problemDetails.LastDetails.Detail);
    }

    [Fact]
    public async Task Sales_and_inventory_use_same_concurrency_error_code()
    {
        var salesProblemDetails = new FakeProblemDetailsService();
        var inventoryProblemDetails = new InventoryFakeProblemDetailsService();
        var sales = CreateMiddleware(salesProblemDetails);
        var inventory = new Inventory.Api.Middleware.ExceptionHandlingMiddleware(
            inventoryProblemDetails,
            new ErrorCatalogResolver(new Inventory.Api.Middleware.InventoryErrorMessageProvider()));

        await sales.TryHandleAsync(new DefaultHttpContext(), new DbUpdateConcurrencyException(), CancellationToken.None);
        await inventory.TryHandleAsync(new DefaultHttpContext(), new DbUpdateConcurrencyException(), CancellationToken.None);

        Assert.Equal(
            salesProblemDetails.LastDetails!.Extensions["code"],
            inventoryProblemDetails.LastDetails!.Extensions["code"]);
    }

    private static ExceptionHandlingMiddleware CreateMiddleware(FakeProblemDetailsService problemDetails)
    {
        return new ExceptionHandlingMiddleware(
            problemDetails,
            new ErrorCatalogResolver(new SalesErrorMessageProvider()));
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

    private sealed class InventoryFakeProblemDetailsService : IProblemDetailsService
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
