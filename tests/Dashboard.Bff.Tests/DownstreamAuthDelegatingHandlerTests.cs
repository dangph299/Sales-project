using System.Net;
using Dashboard.Bff.Auth;

namespace Dashboard.Bff.Tests;

/// <summary>
/// Exercises <see cref="DownstreamAuthDelegatingHandler"/>: caller-token pass-through vs.
/// service-account fallback.
/// </summary>
public sealed class DownstreamAuthDelegatingHandlerTests
{
    [Fact]
    public async Task SendAsync_with_caller_token_forwards_it_and_never_calls_service_token_provider()
    {
        var callerTokenAccessor = new FakeCallerTokenAccessor("caller-jwt");
        var serviceTokenProvider = new FakeServiceTokenProvider("service-jwt");
        var innerHandler = new RecordingHandler();
        var handler = new DownstreamAuthDelegatingHandler(callerTokenAccessor, serviceTokenProvider)
        {
            InnerHandler = innerHandler,
        };
        using var invoker = new HttpMessageInvoker(handler);

        using var request = new HttpRequestMessage(HttpMethod.Get, "http://sales.local/api/orders");
        await invoker.SendAsync(request, CancellationToken.None);

        Assert.NotNull(innerHandler.LastRequest);
        Assert.Equal("Bearer", innerHandler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("caller-jwt", innerHandler.LastRequest.Headers.Authorization!.Parameter);
        Assert.Equal(0, serviceTokenProvider.InvocationCount);
    }

    [Fact]
    public async Task SendAsync_without_caller_token_uses_service_account_token()
    {
        var callerTokenAccessor = new FakeCallerTokenAccessor(token: null);
        var serviceTokenProvider = new FakeServiceTokenProvider("service-jwt");
        var innerHandler = new RecordingHandler();
        var handler = new DownstreamAuthDelegatingHandler(callerTokenAccessor, serviceTokenProvider)
        {
            InnerHandler = innerHandler,
        };
        using var invoker = new HttpMessageInvoker(handler);

        using var request = new HttpRequestMessage(HttpMethod.Get, "http://sales.local/api/orders");
        await invoker.SendAsync(request, CancellationToken.None);

        Assert.NotNull(innerHandler.LastRequest);
        Assert.Equal("Bearer", innerHandler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("service-jwt", innerHandler.LastRequest.Headers.Authorization!.Parameter);
        Assert.Equal(1, serviceTokenProvider.InvocationCount);
    }

    /// <summary>Hand-rolled fake caller token accessor with a fixed <see cref="TryGetToken"/> result.</summary>
    private sealed class FakeCallerTokenAccessor : ICallerTokenAccessor
    {
        private readonly string? _token;

        public FakeCallerTokenAccessor(string? token) => _token = token;

        public bool TryGetToken(out string token)
        {
            token = _token ?? string.Empty;
            return _token is not null;
        }
    }

    /// <summary>Hand-rolled fake service token provider recording invocation count.</summary>
    private sealed class FakeServiceTokenProvider : IServiceTokenProvider
    {
        private readonly string _token;

        public FakeServiceTokenProvider(string token) => _token = token;

        public int InvocationCount { get; private set; }

        public Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            return Task.FromResult(_token);
        }
    }

    /// <summary>Hand-rolled inner handler recording the last outgoing request.</summary>
    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
