using System.Text.Json;
using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using BuildingBlocks.Web.ExceptionHandling;
using BuildingBlocks.Web.Models;
using FluentValidation;
using FluentValidation.Results;
using Inventory.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Inventory.Api.Tests;

public sealed class ApiExceptionHandlerTests
{
    [Fact]
    public async Task Concurrency_exception_maps_to_409()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext();

        var handled = await middleware.TryHandleAsync(context, new DbUpdateConcurrencyException(), CancellationToken.None);
        var response = ReadError(context);

        Assert.True(handled);
        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal(409, response.Status);
        Assert.Equal(ErrorCodes.ConcurrencyConflict, response.ErrorCode);
        Assert.Equal("Inventory was changed by another operation. Please retry.", response.Message);
        AssertApiError(response.Errors!, "retryable", bool.FalseString);
    }

    [Fact]
    public async Task Non_concurrency_data_integrity_exception_maps_to_500()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext();

        var handled = await middleware.TryHandleAsync(context, new DbUpdateException("FK violation"), CancellationToken.None);
        var response = ReadError(context);

        Assert.True(handled);
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal(500, response.Status);
        Assert.Equal(ErrorCodes.InternalServerError, response.ErrorCode);
        Assert.DoesNotContain("FK violation", response.Message);
    }

    [Fact]
    public async Task Validation_exception_maps_to_400_with_shared_code()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext();
        var exception = new ValidationException([new ValidationFailure("Quantity", "Quantity is required.")]);

        var handled = await middleware.TryHandleAsync(context, exception, CancellationToken.None);
        var response = ReadError(context);

        Assert.True(handled);
        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.Validation, response.ErrorCode);
        AssertValidationError(response.ValidationErrors!, "Quantity", "Quantity is required.");
    }

    [Fact]
    public async Task Raw_serialization_failure_from_commit_maps_to_409()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext();
        var serializationFailure = new PostgresException("could not serialize access", "ERROR", "ERROR", "40001");

        var handled = await middleware.TryHandleAsync(context, serializationFailure, CancellationToken.None);
        var response = ReadError(context);

        Assert.True(handled);
        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal(409, response.Status);
        Assert.Equal(ErrorCodes.ConcurrencyConflict, response.ErrorCode);
        AssertApiError(response.Errors!, "retryable", bool.TrueString);
    }

    [Fact]
    public async Task Deadlock_wrapped_in_DbUpdateException_maps_to_409()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext();
        var deadlock = new PostgresException("deadlock detected", "ERROR", "ERROR", "40P01");

        var handled = await middleware.TryHandleAsync(context, new DbUpdateException("save failed", deadlock), CancellationToken.None);
        var response = ReadError(context);

        Assert.True(handled);
        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal(409, response.Status);
        Assert.Equal(ErrorCodes.ConcurrencyConflict, response.ErrorCode);
        AssertApiError(response.Errors!, "retryable", bool.TrueString);
    }

    [Fact]
    public async Task Unique_violation_maps_to_409_with_shared_unique_code()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext();
        var unique = new PostgresException("duplicate key", "ERROR", "ERROR", "23505");

        var handled = await middleware.TryHandleAsync(context, new DbUpdateException("save failed", unique), CancellationToken.None);
        var response = ReadError(context);

        Assert.True(handled);
        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.UniqueViolation, response.ErrorCode);
        AssertApiError(response.Errors!, "retryable", bool.FalseString);
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

    private static ApiExceptionHandler CreateMiddleware()
    {
        var errorCatalog = new ErrorCatalogResolver(new InventoryErrorMessageProvider());
        return new ApiExceptionHandler(
            errorCatalog,
            new PostgresPersistenceExceptionClassifier(),
            Options.Create(new ApiExceptionHandlingOptions()),
            NullLogger<ApiExceptionHandler>.Instance);
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

    private static void AssertApiError(IReadOnlyCollection<ApiError> errors, string code, string message)
    {
        foreach (var error in errors)
        {
            if (error.Code == code && error.Message == message)
            {
                return;
            }
        }

        Assert.Fail($"Expected API error '{code}' with message '{message}'.");
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
