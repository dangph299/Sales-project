using System.Diagnostics;
using BuildingBlocks.Web.Models;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Web.Extensions;

/// <summary>
/// Converts framework validation and HTTP metadata into shared API models.
/// Use these helpers at web boundaries to keep controllers and middleware consistent.
/// </summary>
public static class ApiModelExtensions
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    /// <summary>
    /// Configures ASP.NET Core validation failures to use the shared API error response.
    /// Use it when registering controllers for an API host.
    /// </summary>
    /// <param name="services">Service collection for the API host.</param>
    public static IServiceCollection AddSharedApiModelResponses(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(ConfigureApiBehaviorOptions);

        return services;
    }

    private static void ConfigureApiBehaviorOptions(ApiBehaviorOptions options)
    {
        options.InvalidModelStateResponseFactory = CreateInvalidModelStateResponse;
    }

    private static IActionResult CreateInvalidModelStateResponse(ActionContext context)
    {
        var validationErrors = context.ModelState.ToValidationErrors();
        var response = new ApiErrorResponse(
            StatusCodes.Status400BadRequest,
            "validation",
            "Request validation failed.",
            context.HttpContext.GetTraceId(),
            context.HttpContext.GetCorrelationId(),
            null,
            validationErrors);

        return new BadRequestObjectResult(response);
    }

    /// <summary>
    /// Converts a FluentValidation exception into shared validation errors.
    /// </summary>
    /// <param name="exception">Validation exception raised by FluentValidation.</param>
    /// <returns>Reusable validation errors for API responses.</returns>
    public static IReadOnlyCollection<ValidationError> ToValidationErrors(this ValidationException exception)
    {
        var errors = new List<ValidationError>();

        foreach (var error in exception.Errors)
        {
            errors.Add(new ValidationError(error.PropertyName, error.ErrorMessage, error.ErrorCode));
        }

        return errors;
    }

    /// <summary>
    /// Converts ASP.NET Core model state into shared validation errors.
    /// </summary>
    /// <param name="modelState">Model state produced by model binding.</param>
    /// <returns>Reusable validation errors for API responses.</returns>
    public static IReadOnlyCollection<ValidationError> ToValidationErrors(this ModelStateDictionary modelState)
    {
        var errors = new List<ValidationError>();

        foreach (var entry in modelState)
        {
            if (entry.Value is null || entry.Value.Errors.Count == 0)
            {
                continue;
            }

            foreach (var error in entry.Value.Errors)
            {
                errors.Add(new ValidationError(entry.Key, error.ErrorMessage));
            }
        }

        return errors;
    }

    /// <summary>
    /// Gets the W3C trace identifier for the current request. This is the single definition of
    /// "TraceId" for the whole solution: the value returned to API clients, the value pushed onto
    /// the Serilog request summary, and the value Serilog stamps on every log event from
    /// <see cref="Activity"/> are all this one string, so a client-reported TraceId can be pasted
    /// straight into Seq, Kibana, or any trace backend.
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <returns>The 32-character W3C trace id, or the connection-scoped request id when no <see cref="Activity"/> is running.</returns>
    public static string GetTraceId(this HttpContext context) =>
        Activity.Current?.TraceId.ToHexString() ?? context.TraceIdentifier;

    /// <summary>
    /// Gets the request correlation identifier: the caller-supplied correlation header when present,
    /// otherwise the request's own trace id so the value is never empty. This is the single
    /// definition of "CorrelationId" for the whole solution - responses and logs both resolve it
    /// here, so they can never disagree.
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <returns>The correlation identifier for this request.</returns>
    public static string GetCorrelationId(this HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var value)
            && !string.IsNullOrWhiteSpace(value))
        {
            return value.ToString();
        }

        return context.GetTraceId();
    }

    /// <summary>
    /// Creates a 200 OK response containing an <see cref="ApiResponse{T}"/>.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="controller">Controller creating the response.</param>
    /// <param name="data">Payload returned by the operation.</param>
    /// <param name="message">Optional client-safe response message.</param>
    /// <returns>OK response with the shared API envelope.</returns>
    public static OkObjectResult ToOkResponse<T>(this ControllerBase controller, T data, string? message = null)
    {
        var correlationId = controller.HttpContext.GetCorrelationId();
        var response = ApiResponse<T>.Success(data, message, correlationId);

        return controller.Ok(response);
    }

    /// <summary>
    /// Creates a 201 Created response containing an <see cref="ApiResponse{T}"/>.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="controller">Controller creating the response.</param>
    /// <param name="uri">Location of the created resource.</param>
    /// <param name="data">Payload returned by the operation.</param>
    /// <param name="message">Optional client-safe response message.</param>
    /// <returns>Created response with the shared API envelope.</returns>
    public static CreatedResult ToCreatedResponse<T>(this ControllerBase controller, string uri, T data, string? message = null)
    {
        var correlationId = controller.HttpContext.GetCorrelationId();
        var response = ApiResponse<T>.Success(data, message, correlationId);

        return controller.Created(uri, response);
    }

    /// <summary>
    /// Creates a 204 No Content response.
    /// </summary>
    /// <param name="controller">Controller creating the response.</param>
    /// <returns>No-content response.</returns>
    public static NoContentResult ToNoContentResponse(this ControllerBase controller)
    {
        return controller.NoContent();
    }
}
