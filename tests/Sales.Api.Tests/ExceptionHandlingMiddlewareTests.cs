using System.Text.Json;
using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using BuildingBlocks.Web.Models;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
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
        var middleware = CreateMiddleware();
        var context = CreateContext();

        var handled = await middleware.TryHandleAsync(context, new DbUpdateConcurrencyException(), CancellationToken.None);
        var response = ReadError(context);

        Assert.True(handled);
        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.ConcurrencyConflict, response.ErrorCode);
        Assert.Equal("The sales resource was changed by another request.", response.Message);
    }

    [Fact]
    public async Task Not_found_exception_maps_to_404_with_shared_code()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext();

        var handled = await middleware.TryHandleAsync(context, new NotFoundException("Order", Guid.NewGuid()), CancellationToken.None);
        var response = ReadError(context);

        Assert.True(handled);
        Assert.Equal(404, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.NotFound, response.ErrorCode);
    }

    [Fact]
    public async Task Validation_exception_maps_to_400_with_shared_code()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext();
        var exception = new ValidationException([new ValidationFailure("Name", "Name is required.")]);

        var handled = await middleware.TryHandleAsync(context, exception, CancellationToken.None);
        var response = ReadError(context);

        Assert.True(handled);
        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.Validation, response.ErrorCode);
        AssertValidationError(response.ValidationErrors!, "Name", "Name is required.");
    }

    [Fact]
    public async Task Unique_violation_maps_to_409_with_shared_code()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext();
        var unique = new PostgresException("duplicate key", "ERROR", "ERROR", "23505");

        var handled = await middleware.TryHandleAsync(context, new DbUpdateException("save failed", unique), CancellationToken.None);
        var response = ReadError(context);

        Assert.True(handled);
        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.UniqueViolation, response.ErrorCode);
    }

    [Fact]
    public async Task Unknown_DbUpdateException_maps_to_500()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext();

        var handled = await middleware.TryHandleAsync(context, new DbUpdateException("FK violation"), CancellationToken.None);
        var response = ReadError(context);

        Assert.True(handled);
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.InternalServerError, response.ErrorCode);
        Assert.DoesNotContain("FK violation", response.Message);
    }

    [Fact]
    public async Task Unknown_exception_maps_to_500_without_internal_detail()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext();

        var handled = await middleware.TryHandleAsync(context, new InvalidOperationException("secret internals"), CancellationToken.None);
        var response = ReadError(context);

        Assert.True(handled);
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.InternalServerError, response.ErrorCode);
        Assert.DoesNotContain("secret internals", response.Message);
    }

    [Fact]
    public async Task Sales_and_inventory_use_same_concurrency_error_code()
    {
        var sales = CreateMiddleware();
        var inventory = new Inventory.Api.Middleware.ExceptionHandlingMiddleware(
            new ErrorCatalogResolver(new Inventory.Api.Middleware.InventoryErrorMessageProvider()),
            new PostgresPersistenceExceptionClassifier());
        var salesContext = CreateContext();
        var inventoryContext = CreateContext();

        await sales.TryHandleAsync(salesContext, new DbUpdateConcurrencyException(), CancellationToken.None);
        await inventory.TryHandleAsync(inventoryContext, new DbUpdateConcurrencyException(), CancellationToken.None);

        Assert.Equal(
            ReadError(salesContext).ErrorCode,
            ReadError(inventoryContext).ErrorCode);
    }

    private static ExceptionHandlingMiddleware CreateMiddleware()
    {
        return new ExceptionHandlingMiddleware(
            new ErrorCatalogResolver(new SalesErrorMessageProvider()),
            new PostgresPersistenceExceptionClassifier());
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static ApiErrorResponse ReadError(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return JsonSerializer.Deserialize<ApiErrorResponse>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }

    private static void AssertValidationError(IReadOnlyCollection<ValidationError> validationErrors, string field, string message)
    {
        foreach (var validationError in validationErrors)
        {
            if (validationError.Field == field && validationError.Message == message)
            {
                return;
            }
        }

        Assert.Fail($"Expected validation error for field '{field}' with message '{message}'.");
    }
}
