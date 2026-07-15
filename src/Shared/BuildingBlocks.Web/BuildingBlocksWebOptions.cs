using BuildingBlocks.Web.ExceptionHandling;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Web;

/// <summary>
/// Configures the shared API host composition applied by
/// <see cref="WebHostRegistration.AddBuildingBlocksWeb"/>. Carries only the service identity and
/// the hooks each service needs to specialise the shared pipeline; capability-specific settings
/// (Kafka, persistence, and similar) stay with their own options.
/// </summary>
public sealed class BuildingBlocksWebOptions
{
    /// <summary>Service name used for logging and the OpenTelemetry resource.</summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>API title shown in the OpenAPI document.</summary>
    public string ApiTitle { get; set; } = string.Empty;

    /// <summary>API description shown in the OpenAPI document.</summary>
    public string ApiDescription { get; set; } = string.Empty;

    /// <summary>Activity source name added to the tracing pipeline.</summary>
    public string ActivitySourceName { get; set; } = string.Empty;

    /// <summary>Meter name added to the metrics pipeline.</summary>
    public string MeterName { get; set; } = string.Empty;

    /// <summary>Optional JWT clock-skew override; ASP.NET Core's default is used when null.</summary>
    public TimeSpan? JwtClockSkew { get; set; }

    /// <summary>Optional service-specific exception-to-response mappings.</summary>
    public Action<ApiExceptionHandlingOptions>? ConfigureExceptions { get; set; }

    /// <summary>Optional hook to further configure the MVC controllers builder (e.g. JSON options).</summary>
    public Action<IMvcBuilder>? ConfigureControllers { get; set; }

    internal void Validate()
    {
        ThrowIfBlank(ServiceName, nameof(ServiceName));
        ThrowIfBlank(ApiTitle, nameof(ApiTitle));
        ThrowIfBlank(ApiDescription, nameof(ApiDescription));
        ThrowIfBlank(ActivitySourceName, nameof(ActivitySourceName));
        ThrowIfBlank(MeterName, nameof(MeterName));
    }

    private static void ThrowIfBlank(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{nameof(BuildingBlocksWebOptions)}.{propertyName} must be set.");
        }
    }
}
