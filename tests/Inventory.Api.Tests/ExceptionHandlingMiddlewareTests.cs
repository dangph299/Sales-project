using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using FluentValidation;
using FluentValidation.Results;
using Inventory.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Inventory.Api.Tests;

public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task Concurrency_exception_maps_to_409()
    {
        var problemDetails = new FakeProblemDetailsService();
        var middleware = CreateMiddleware(problemDetails);
        var context = new DefaultHttpContext();

        var handled = await middleware.TryHandleAsync(context, new DbUpdateConcurrencyException(), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal(409, problemDetails.LastDetails!.Status);
        Assert.Equal(ErrorCodes.ConcurrencyConflict, problemDetails.LastDetails.Extensions["code"]);
        Assert.Equal("Inventory was changed by another operation. Please retry.", problemDetails.LastDetails.Extensions["description"]);
        Assert.Equal(false, problemDetails.LastDetails.Extensions["retryable"]);
    }

    [Fact]
    public async Task Non_concurrency_data_integrity_exception_maps_to_500()
    {
        var problemDetails = new FakeProblemDetailsService();
        var middleware = CreateMiddleware(problemDetails);
        var context = new DefaultHttpContext();

        var handled = await middleware.TryHandleAsync(context, new DbUpdateException("FK violation"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal(500, problemDetails.LastDetails!.Status);
        Assert.Equal(ErrorCodes.InternalServerError, problemDetails.LastDetails.Extensions["code"]);
        Assert.Null(problemDetails.LastDetails.Detail);
    }

    [Fact]
    public async Task Validation_exception_maps_to_400_with_shared_code()
    {
        var problemDetails = new FakeProblemDetailsService();
        var middleware = CreateMiddleware(problemDetails);
        var context = new DefaultHttpContext();
        var exception = new ValidationException([new ValidationFailure("Quantity", "Quantity is required.")]);

        var handled = await middleware.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.Validation, problemDetails.LastDetails!.Extensions["code"]);
        Assert.True(problemDetails.LastDetails.Extensions.ContainsKey("errors"));
    }

    [Fact]
    public async Task Raw_serialization_failure_from_commit_maps_to_409()
    {
        var problemDetails = new FakeProblemDetailsService();
        var middleware = CreateMiddleware(problemDetails);
        var context = new DefaultHttpContext();
        var serializationFailure = new PostgresException("could not serialize access", "ERROR", "ERROR", "40001");

        var handled = await middleware.TryHandleAsync(context, serializationFailure, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal(409, problemDetails.LastDetails!.Status);
        Assert.Equal(ErrorCodes.ConcurrencyConflict, problemDetails.LastDetails.Extensions["code"]);
        Assert.Equal(true, problemDetails.LastDetails.Extensions["retryable"]);
    }

    [Fact]
    public async Task Deadlock_wrapped_in_DbUpdateException_maps_to_409()
    {
        var problemDetails = new FakeProblemDetailsService();
        var middleware = CreateMiddleware(problemDetails);
        var context = new DefaultHttpContext();
        var deadlock = new PostgresException("deadlock detected", "ERROR", "ERROR", "40P01");

        var handled = await middleware.TryHandleAsync(context, new DbUpdateException("save failed", deadlock), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal(409, problemDetails.LastDetails!.Status);
        Assert.Equal(ErrorCodes.ConcurrencyConflict, problemDetails.LastDetails.Extensions["code"]);
        Assert.Equal(true, problemDetails.LastDetails.Extensions["retryable"]);
    }

    [Fact]
    public async Task Unique_violation_maps_to_409_with_shared_unique_code()
    {
        var problemDetails = new FakeProblemDetailsService();
        var middleware = CreateMiddleware(problemDetails);
        var context = new DefaultHttpContext();
        var unique = new PostgresException("duplicate key", "ERROR", "ERROR", "23505");

        var handled = await middleware.TryHandleAsync(context, new DbUpdateException("save failed", unique), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.UniqueViolation, problemDetails.LastDetails!.Extensions["code"]);
        Assert.Equal(false, problemDetails.LastDetails.Extensions["retryable"]);
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

    private static ExceptionHandlingMiddleware CreateMiddleware(FakeProblemDetailsService problemDetails)
    {
        return new ExceptionHandlingMiddleware(
            problemDetails,
            new ErrorCatalogResolver(new InventoryErrorMessageProvider()),
            new PostgresPersistenceExceptionClassifier());
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
