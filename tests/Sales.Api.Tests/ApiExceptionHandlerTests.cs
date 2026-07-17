using System.Text.Json;
using BuildingBlocks.Contracts;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure;
using BuildingBlocks.Web.ExceptionHandling;
using BuildingBlocks.Web.Models;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Sales.Api.Middleware;
using Sales.Application.Common.Exceptions;

namespace Sales.Api.Tests;

public sealed class ApiExceptionHandlerTests
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
    public async Task Unauthorized_exception_maps_to_401()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext();

        var handled = await middleware.TryHandleAsync(context, new UnauthorizedAccessException(), CancellationToken.None);
        var response = ReadError(context);

        Assert.True(handled);
        Assert.Equal(401, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.Unauthorized, response.ErrorCode);
    }

    [Fact]
    public async Task Domain_exception_maps_to_400_with_shared_code()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext();

        var handled = await middleware.TryHandleAsync(context, new DomainException("Only a draft order can be edited."), CancellationToken.None);
        var response = ReadError(context);

        Assert.True(handled);
        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.InvalidOperation, response.ErrorCode);
    }

    [Fact]
    public async Task Domain_exception_subclass_maps_to_400()
    {
        // Guards the latent bug in the old string-name check, which only inspected one base-type level
        // and would map a nested DomainException subclass to 500 instead of 400.
        var middleware = CreateMiddleware();
        var context = CreateContext();

        var handled = await middleware.TryHandleAsync(context, new NestedDomainException(), CancellationToken.None);
        var response = ReadError(context);

        Assert.True(handled);
        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.InvalidOperation, response.ErrorCode);
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
    public async Task Raw_serialization_failure_maps_to_409()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext();
        var serializationFailure = new PostgresException("could not serialize access", "ERROR", "ERROR", "40001");

        var handled = await middleware.TryHandleAsync(context, serializationFailure, CancellationToken.None);
        var response = ReadError(context);

        Assert.True(handled);
        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.ConcurrencyConflict, response.ErrorCode);
    }

    [Fact]
    public async Task Foreign_key_violation_maps_to_500_without_database_detail()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext();
        var foreignKeyViolation = new PostgresException("insert violates foreign key", "ERROR", "ERROR", "23503");

        var handled = await middleware.TryHandleAsync(context, new DbUpdateException("save failed", foreignKeyViolation), CancellationToken.None);
        var response = ReadError(context);

        Assert.True(handled);
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.InternalServerError, response.ErrorCode);
        Assert.DoesNotContain("foreign key", response.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("23503", response.Message, StringComparison.OrdinalIgnoreCase);
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
    public async Task Unknown_exception_is_logged_once()
    {
        var logger = new RecordingLogger<ApiExceptionHandler>();
        var middleware = CreateMiddleware(logger);
        var context = CreateContext();

        await middleware.TryHandleAsync(context, new InvalidOperationException("secret internals"), CancellationToken.None);

        Assert.Equal(1, logger.ErrorCount);
    }

    [Fact]
    public async Task Sales_and_inventory_use_same_concurrency_error_code()
    {
        var sales = CreateMiddleware();
        var inventory = CreateInventoryMiddleware();
        var salesContext = CreateContext();
        var inventoryContext = CreateContext();

        await sales.TryHandleAsync(salesContext, new DbUpdateConcurrencyException(), CancellationToken.None);
        await inventory.TryHandleAsync(inventoryContext, new DbUpdateConcurrencyException(), CancellationToken.None);

        Assert.Equal(
            ReadError(salesContext).ErrorCode,
            ReadError(inventoryContext).ErrorCode);
    }

    private static ApiExceptionHandler CreateMiddleware(ILogger<ApiExceptionHandler>? logger = null)
    {
        var errorCatalog = new ErrorCatalogResolver(new SalesErrorMessageProvider());
        var options = new ApiExceptionHandlingOptions();
        options.Map<DomainException>((_, catalog) =>
        {
            var error = catalog.Get(ErrorCodes.InvalidOperation);
            return new ApiExceptionMapping(400, error.Code, error.Description);
        });

        options.Map<NotFoundException>((_, catalog) =>
        {
            var error = catalog.Get(ErrorCodes.NotFound);
            return new ApiExceptionMapping(404, error.Code, error.Description);
        });

        options.Map<ConflictException>((exception, catalog) =>
        {
            var error = catalog.Get(ErrorCodes.ConcurrencyConflict);
            var errors = new[] { new ApiError("current_version", exception.CurrentVersion.ToString()) };
            return new ApiExceptionMapping(409, error.Code, error.Description, errors);
        });

        return new ApiExceptionHandler(
            errorCatalog,
            new PostgresPersistenceExceptionClassifier(),
            Options.Create(options),
            logger ?? NullLogger<ApiExceptionHandler>.Instance);
    }

    private static ApiExceptionHandler CreateInventoryMiddleware()
    {
        var errorCatalog = new ErrorCatalogResolver(new Inventory.Api.Middleware.InventoryErrorMessageProvider());
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

    private class IntermediateDomainException(string message) : DomainException(message);

    private sealed class NestedDomainException() : IntermediateDomainException("nested domain rule violated");

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public int ErrorCount { get; private set; }

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error)
            {
                ErrorCount++;
            }
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
