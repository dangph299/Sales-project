using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Application;
using Dashboard.Bff.Auth;
using Dashboard.Bff.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dashboard.Bff.Tests;

/// <summary>
/// Exercises <see cref="ServiceAccountTokenProvider"/>: login, caching, and re-login on expiry.
/// </summary>
public sealed class ServiceAccountTokenProviderTests
{
    private const string EnvelopeJson =
        """
        {
          "success": true,
          "message": null,
          "correlationId": "11111111-1111-1111-1111-111111111111",
          "data": { "accessToken": "the-jwt", "expiresIn": 1800, "refreshToken": "the-refresh-token" }
        }
        """;

    private static ServiceAccountTokenProvider CreateProvider(
        FakeHttpMessageHandler handler,
        FakeClock clock,
        string userName = "svc-user",
        string password = "svc-pass")
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://sales.local") };
        var options = Microsoft.Extensions.Options.Options.Create(new ServiceAccountOptions { UserName = userName, Password = password });
        return new ServiceAccountTokenProvider(
            httpClient,
            options,
            clock,
            NullLogger<ServiceAccountTokenProvider>.Instance);
    }

    [Fact]
    public async Task GetTokenAsync_logs_in_and_returns_access_token()
    {
        var handler = new FakeHttpMessageHandler(EnvelopeJson);
        var clock = new FakeClock(DateTimeOffset.Parse("2026-07-23T00:00:00Z"));
        var provider = CreateProvider(handler, clock);

        var token = await provider.GetTokenAsync();

        Assert.Equal("the-jwt", token);
        Assert.Equal(1, handler.InvocationCount);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/api/auth/login", handler.LastRequest.RequestUri!.AbsolutePath);

        var sentBody = await handler.LastRequestContent!.ReadAsStringAsync();
        using var document = JsonDocument.Parse(sentBody);
        Assert.Equal("svc-user", document.RootElement.GetProperty("userName").GetString());
    }

    [Fact]
    public async Task GetTokenAsync_within_ttl_returns_cached_token_without_second_http_call()
    {
        var handler = new FakeHttpMessageHandler(EnvelopeJson);
        var clock = new FakeClock(DateTimeOffset.Parse("2026-07-23T00:00:00Z"));
        var provider = CreateProvider(handler, clock);

        var first = await provider.GetTokenAsync();
        var second = await provider.GetTokenAsync();

        Assert.Equal("the-jwt", first);
        Assert.Equal("the-jwt", second);
        Assert.Equal(1, handler.InvocationCount);
    }

    [Fact]
    public async Task GetTokenAsync_after_expiry_triggers_second_login()
    {
        var handler = new FakeHttpMessageHandler(EnvelopeJson);
        var clock = new FakeClock(DateTimeOffset.Parse("2026-07-23T00:00:00Z"));
        var provider = CreateProvider(handler, clock);

        await provider.GetTokenAsync();

        // expiresIn = 1800s, minus 60s safety margin => cached expiry at +1740s.
        clock.UtcNow = clock.UtcNow.AddSeconds(1800);

        var second = await provider.GetTokenAsync();

        Assert.Equal("the-jwt", second);
        Assert.Equal(2, handler.InvocationCount);
    }

    /// <summary>Hand-rolled fake HTTP handler recording invocations and returning a canned response.</summary>
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public FakeHttpMessageHandler(string responseJson) => _responseJson = responseJson;

        public int InvocationCount { get; private set; }

        public HttpRequestMessage? LastRequest { get; private set; }

        public HttpContent? LastRequestContent { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            LastRequest = request;
            LastRequestContent = request.Content;
            if (request.Content is not null)
            {
                // Buffer so the content can still be read after this fake handler returns.
                var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
                LastRequestContent = new ByteArrayContent(bytes);
                foreach (var header in request.Content.Headers)
                {
                    LastRequestContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    /// <summary>Hand-rolled fake clock with a settable <see cref="UtcNow"/>.</summary>
    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTimeOffset initial) => UtcNow = initial;

        public DateTimeOffset UtcNow { get; set; }
    }
}
