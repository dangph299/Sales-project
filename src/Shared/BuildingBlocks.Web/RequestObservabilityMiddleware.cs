using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Web.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;

namespace BuildingBlocks.Web;

/// <summary>
/// one shared HTTP middleware every service uses instead of a per-service copy.
/// Owns everything the single Serilog request-logging completion event needs:
/// RequestId/CorrelationId/TraceId/UserId/ClientIp on <see cref="IDiagnosticContext"/> (so it
/// reaches that one event regardless of middleware nesting) and on <see cref="LogContext"/>
/// (so nested business/Kafka logs inherit it too). Request/response bodies are captured only
/// when Debug is enabled - never mixed into the Information-level summary.
/// </summary>
public sealed class RequestObservabilityMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<RequestObservabilityMiddleware> logger)
{
    private static readonly string[] DefaultSensitiveHeaders = ["Authorization", "Cookie", "Set-Cookie"];
    private static readonly string[] DefaultSensitiveJsonFields = ["password", "token", "accesstoken", "refreshtoken", "secret", "currentpassword", "newpassword"];

    /// <summary>
    /// Enriches the request's log scope and the request-logging summary event with correlation,
    /// trace, user, and (at Debug level) request/response body information.
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="diagnosticContext">Serilog diagnostic context that <c>RequestLoggingMiddleware</c> reads when it writes the request summary event.</param>
    public async Task InvokeAsync(HttpContext context, IDiagnosticContext diagnosticContext)
    {
        var requestId = context.TraceIdentifier;
        var traceId = context.GetTraceId();
        var correlationId = context.GetCorrelationId();

        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            var debugEnabled = logger.IsEnabled(LogLevel.Debug);
            var logRequestBody = debugEnabled && configuration.GetValue("HttpLogging:LogRequestBody", true);
            var logResponseBody = debugEnabled && configuration.GetValue("HttpLogging:LogResponseBody", false);
            var maxBodyBytes = debugEnabled ? configuration.GetValue("HttpLogging:MaxBodyBytes", 8192) : 0;
            var captureBodies = logRequestBody || logResponseBody;

            var request = context.Request;
            string? requestBody = null;
            if (captureBodies && logRequestBody && IsTextBody(request.ContentType))
            {
                request.EnableBuffering();
                requestBody = await ReadAndCapAsync(request.Body, maxBodyBytes);
                request.Body.Position = 0;
            }

            var originalResponseBody = context.Response.Body;
            var responseBuffer = captureBodies && logResponseBody ? new MemoryStream() : null;
            if (responseBuffer is not null) context.Response.Body = responseBuffer;

            try
            {
                await next(context);
            }
            finally
            {
                if (responseBuffer is not null) context.Response.Body = originalResponseBody;

                diagnosticContext.Set("RequestId", requestId);
                diagnosticContext.Set("CorrelationId", correlationId);
                diagnosticContext.Set("TraceId", traceId);
                diagnosticContext.Set("UserId", context.User.Identity?.IsAuthenticated == true
                    ? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    : null);
                diagnosticContext.Set("ClientIp", context.Connection.RemoteIpAddress?.ToString());
                diagnosticContext.Set("Url", request.GetDisplayUrl());
                diagnosticContext.Set("Route", (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText);
                diagnosticContext.Set("UserAgent", request.Headers.UserAgent.ToString());

                if (captureBodies)
                {
                    var sensitiveHeaders = configuration.GetSection("HttpLogging:SensitiveHeaders").Get<string[]>() ?? DefaultSensitiveHeaders;
                    var sensitiveJsonFields = configuration.GetSection("HttpLogging:SensitiveJsonFields").Get<string[]>() ?? DefaultSensitiveJsonFields;
                    string? responseText = null;
                    if (responseBuffer is not null)
                        responseText = await CopyAndCapAsync(responseBuffer, maxBodyBytes, originalResponseBody);

                    logger.LogDebug(
                        "HTTP body {Method} {Path} Headers={RequestHeaders} Request={RequestBody} Response={ResponseBody}",
                        request.Method, request.GetDisplayUrl(),
                        MaskHeaders(request.Headers, sensitiveHeaders),
                        requestBody is null ? null : MaskSensitiveJson(requestBody, sensitiveJsonFields),
                        responseText is null ? null : MaskSensitiveJson(responseText, sensitiveJsonFields));
                }
            }
        }
    }

    private static bool IsTextBody(string? contentType) =>
        contentType is not null && (contentType.Contains("json", StringComparison.OrdinalIgnoreCase) || contentType.Contains("text", StringComparison.OrdinalIgnoreCase));

    private static async Task<string> ReadAndCapAsync(Stream body, int maxBytes)
    {
        using var reader = new StreamReader(body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: -1, leaveOpen: true);
        var buffer = new char[maxBytes];
        var read = await reader.ReadBlockAsync(buffer, 0, maxBytes);
        var text = new string(buffer, 0, read);
        return read == maxBytes ? text + "...[truncated]" : text;
    }

    private static async Task<string?> CopyAndCapAsync(MemoryStream buffer, int maxBytes, Stream destination)
    {
        buffer.Position = 0;
        await buffer.CopyToAsync(destination);
        if (buffer.Length == 0) return null;

        buffer.Position = 0;
        var length = (int)Math.Min(buffer.Length, maxBytes);
        var bytes = new byte[length];
        _ = await buffer.ReadAsync(bytes.AsMemory(0, length));
        var text = Encoding.UTF8.GetString(bytes);
        return buffer.Length > maxBytes ? text + "...[truncated]" : text;
    }

    private static Dictionary<string, string> MaskHeaders(IHeaderDictionary headers, string[] sensitiveHeaders) =>
        headers.ToDictionary(
            h => h.Key,
            h => sensitiveHeaders.Any(s => string.Equals(s, h.Key, StringComparison.OrdinalIgnoreCase)) ? "***" : h.Value.ToString());

    private static string MaskSensitiveJson(string json, string[] sensitiveFields)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
                WriteMasked(document.RootElement, writer, sensitiveFields);
            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private static void WriteMasked(JsonElement element, Utf8JsonWriter writer, string[] sensitiveFields)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    if (sensitiveFields.Any(s => string.Equals(s, property.Name, StringComparison.OrdinalIgnoreCase)))
                        writer.WriteStringValue("***");
                    else
                        WriteMasked(property.Value, writer, sensitiveFields);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray()) WriteMasked(item, writer, sensitiveFields);
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }
}
