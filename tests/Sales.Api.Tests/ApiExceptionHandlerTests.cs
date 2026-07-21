using System.Diagnostics;
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
        Assert.Equal("Only a draft order can be edited.", response.Message);
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

    [Theory]
    [MemberData(nameof(LeakyExceptions))]
    public async Task No_response_path_leaks_internal_detail_to_the_client(string name, Exception exception)
    {
        // Scans the whole serialised body, not just Message: the errors[] array is a response path too.
        _ = name;
        var middleware = CreateMiddleware();
        var context = CreateContext();

        await middleware.TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        Assert.DoesNotContain("Npgsql", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PostgresException", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DbUpdate", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("23503", body, StringComparison.Ordinal);
        Assert.DoesNotContain("relation \"orders\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret internals", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("at Sales.", body, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
    }

    public static TheoryData<string, Exception> LeakyExceptions()
    {
        var foreignKey = new PostgresException(
            "insert or update on table \"orders\" violates foreign key constraint", "ERROR", "ERROR", "23503");

        return new TheoryData<string, Exception>
        {
            { "sql-foreign-key", new DbUpdateException("save failed", foreignKey) },
            { "raw-postgres", new PostgresException("relation \"orders\" does not exist", "ERROR", "ERROR", "42P01") },
            { "framework", new InvalidOperationException("secret internals") },
            { "with-stack-trace", CreateThrownException() },
        };
    }

    private static Exception CreateThrownException()
    {
        try
        {
            throw new InvalidOperationException("secret internals");
        }
        catch (InvalidOperationException ex)
        {
            return ex;
        }
    }

    [Fact]
    public async Task Response_trace_id_is_the_w3c_trace_id_a_client_can_search_in_seq()
    {
        // The whole point of the TraceId field: what the client is handed must be the same string
        // Serilog stamps on every log event from Activity.Current, so it can be pasted into Seq/Kibana.
        var middleware = CreateMiddleware();
        var context = CreateContext();
        using var activity = new Activity("test-request").Start();

        await middleware.TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);
        var response = ReadError(context);

        Assert.Equal(activity.TraceId.ToHexString(), response.TraceId);
        Assert.Equal(32, response.TraceId!.Length);
    }

    [Fact]
    public async Task Response_trace_id_falls_back_to_request_id_when_no_activity_is_running()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext();
        context.TraceIdentifier = "0HN7ABC:00000003";
        Activity.Current = null;

        await middleware.TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);
        var response = ReadError(context);

        Assert.Equal("0HN7ABC:00000003", response.TraceId);
    }

    [Fact]
    public async Task Correlation_id_falls_back_to_trace_id_so_it_is_never_null()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext();
        using var activity = new Activity("test-request").Start();

        await middleware.TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);
        var response = ReadError(context);

        Assert.Equal(activity.TraceId.ToHexString(), response.CorrelationId);
    }

    [Fact]
    public async Task Correlation_id_uses_the_caller_supplied_header_when_present()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext();
        context.Request.Headers["X-Correlation-Id"] = "caller-supplied-id";
        using var activity = new Activity("test-request").Start();

        await middleware.TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);
        var response = ReadError(context);

        Assert.Equal("caller-supplied-id", response.CorrelationId);
        Assert.Equal(activity.TraceId.ToHexString(), response.TraceId);
    }

    [Fact]
    public async Task Failure_log_carries_the_error_code_and_status_as_queryable_properties()
    {
        var logger = new RecordingLogger<ApiExceptionHandler>();
        var middleware = CreateMiddleware(logger);
        var context = CreateContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/orders";
        using var activity = new Activity("test-request").Start();

        await middleware.TryHandleAsync(context, new NotFoundException("Order", Guid.NewGuid()), CancellationToken.None);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(ErrorCodes.NotFound, entry.Properties["ErrorCode"]);
        Assert.Equal(404, entry.Properties["StatusCode"]);
        Assert.Equal("POST", entry.Properties["RequestMethod"]);
        Assert.Equal("/api/orders", entry.Properties["RequestPath"]);
        Assert.Equal(activity.TraceId.ToHexString(), entry.Properties["TraceId"]);
        Assert.Equal(activity.TraceId.ToHexString(), entry.Properties["CorrelationId"]);
    }

    [Theory]
    [InlineData(typeof(NotFoundException), LogLevel.Information)]
    [InlineData(typeof(DomainException), LogLevel.Information)]
    [InlineData(typeof(ValidationException), LogLevel.Information)]
    [InlineData(typeof(DbUpdateConcurrencyException), LogLevel.Warning)]
    [InlineData(typeof(InvalidOperationException), LogLevel.Error)]
    public async Task Failures_are_logged_at_the_severity_their_category_deserves(Type exceptionType, LogLevel expected)
    {
        // Client-caused failures must not burn Warning/Error, or the levels stop meaning anything
        // and the conflicts that do need attention get lost in the noise.
        var logger = new RecordingLogger<ApiExceptionHandler>();
        var middleware = CreateMiddleware(logger);
        var context = CreateContext();

        await middleware.TryHandleAsync(context, CreateException(exceptionType), CancellationToken.None);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(expected, entry.Level);
    }

    [Fact]
    public async Task Every_handled_failure_is_logged_exactly_once_with_its_exception_attached()
    {
        var logger = new RecordingLogger<ApiExceptionHandler>();
        var middleware = CreateMiddleware(logger);
        var context = CreateContext();

        await middleware.TryHandleAsync(context, new NotFoundException("Order", Guid.NewGuid()), CancellationToken.None);

        var entry = Assert.Single(logger.Entries);
        Assert.IsType<NotFoundException>(entry.Exception);
    }

    [Fact]
    public async Task Server_faults_mark_the_span_as_error_but_client_faults_do_not()
    {
        // 4xx is the caller's problem. Marking it Error would make every validation failure count
        // against the service's trace error rate.
        var middleware = CreateMiddleware();

        using (var serverFault = new Activity("server").Start())
        {
            await middleware.TryHandleAsync(CreateContext(), new InvalidOperationException("boom"), CancellationToken.None);
            Assert.Equal(ActivityStatusCode.Error, serverFault.Status);
        }

        using var clientFault = new Activity("client").Start();
        await middleware.TryHandleAsync(CreateContext(), new NotFoundException("Order", Guid.NewGuid()), CancellationToken.None);
        Assert.NotEqual(ActivityStatusCode.Error, clientFault.Status);
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
        // Bind the host's real mapping configuration rather than a copy, so these tests fail when
        // ConfigureSalesExceptions changes.
        var options = new ApiExceptionHandlingOptions();
        Sales.Api.Extensions.ServiceCollectionExtensions.ConfigureSalesExceptions(options);

        return new ApiExceptionHandler(
            new ErrorCatalogResolver(new SalesErrorMessageProvider()),
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

    private static Exception CreateException(Type exceptionType)
    {
        if (exceptionType == typeof(NotFoundException)) return new NotFoundException("Order", Guid.NewGuid());
        if (exceptionType == typeof(DomainException)) return new DomainException("Only a draft order can be edited.");
        if (exceptionType == typeof(ValidationException)) return new ValidationException([new ValidationFailure("Name", "Name is required.")]);
        if (exceptionType == typeof(DbUpdateConcurrencyException)) return new DbUpdateConcurrencyException();
        if (exceptionType == typeof(InvalidOperationException)) return new InvalidOperationException("boom");

        throw new ArgumentOutOfRangeException(nameof(exceptionType), exceptionType, "Unmapped test exception type.");
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

    private sealed record LogEntry(
        LogLevel Level,
        Exception? Exception,
        IReadOnlyDictionary<string, object?> Properties);

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public int ErrorCount => Entries.Count(x => x.Level == LogLevel.Error);

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
            var properties = state is IReadOnlyList<KeyValuePair<string, object?>> values
                ? values.ToDictionary(x => x.Key, x => x.Value)
                : [];

            Entries.Add(new LogEntry(logLevel, exception, properties));
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
