using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Application;
using Dashboard.Bff.Options;
using Microsoft.Extensions.Options;

namespace Dashboard.Bff.Auth;

/// <summary>
/// Logs in as the configured service account and caches the resulting access token until shortly
/// before it expires, transparently re-logging in as needed.
/// </summary>
public sealed class ServiceAccountTokenProvider : IServiceTokenProvider
{
    private static readonly TimeSpan ExpirySafetyMargin = TimeSpan.FromSeconds(60);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IOptions<ServiceAccountOptions> _options;
    private readonly IClock _clock;
    private readonly ILogger<ServiceAccountTokenProvider> _logger;
    private readonly SemaphoreSlim _loginLock = new(1, 1);

    private string? _cachedToken;
    private DateTimeOffset _cachedTokenExpiresAt = DateTimeOffset.MinValue;

    public ServiceAccountTokenProvider(
        HttpClient httpClient,
        IOptions<ServiceAccountOptions> options,
        IClock clock,
        ILogger<ServiceAccountTokenProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>
    /// Returns a cached, unexpired access token, logging in again if there is none or it has
    /// expired.
    /// </summary>
    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedToken is not null && _clock.UtcNow < _cachedTokenExpiresAt)
        {
            return _cachedToken;
        }

        await _loginLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedToken is not null && _clock.UtcNow < _cachedTokenExpiresAt)
            {
                return _cachedToken;
            }

            return await LoginAsync(cancellationToken);
        }
        finally
        {
            _loginLock.Release();
        }
    }

    private async Task<string> LoginAsync(CancellationToken cancellationToken)
    {
        var credentials = _options.Value;
        var (userName, password) = ResolveCredentials(credentials);
        _logger.LogInformation("Logging in service account {UserName}", userName);

        var request = new LoginRequest(userName, password);
        using var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<LoginEnvelope>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Service account login returned an empty response.");

        if (!envelope.Success || envelope.Data is null)
        {
            throw new InvalidOperationException($"Service account login failed: {envelope.Message}");
        }

        _cachedToken = envelope.Data.AccessToken;
        _cachedTokenExpiresAt = _clock.UtcNow + TimeSpan.FromSeconds(envelope.Data.ExpiresIn) - ExpirySafetyMargin;

        return _cachedToken;
    }

    private static (string UserName, string Password) ResolveCredentials(ServiceAccountOptions credentials)
    {
        if (credentials.AllowAdminDevFallback
            && string.IsNullOrWhiteSpace(credentials.UserName)
            && string.IsNullOrWhiteSpace(credentials.Password))
        {
            return ("admin", "Admin123!");
        }

        return (credentials.UserName, credentials.Password);
    }

    private sealed record LoginRequest(string UserName, string Password);

    private sealed record LoginEnvelope(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("correlationId")] string? CorrelationId,
        [property: JsonPropertyName("data")] LoginData? Data);

    private sealed record LoginData(string AccessToken, int ExpiresIn, string RefreshToken);
}
