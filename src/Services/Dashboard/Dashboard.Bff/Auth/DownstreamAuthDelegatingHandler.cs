using System.Net.Http.Headers;

namespace Dashboard.Bff.Auth;

/// <summary>
/// Attaches a bearer token to outgoing downstream requests: the original caller's forwarded
/// token when one is available, otherwise the Dashboard BFF's service-account token.
/// </summary>
public sealed class DownstreamAuthDelegatingHandler : DelegatingHandler
{
    private readonly ICallerTokenAccessor _callerTokenAccessor;
    private readonly IServiceTokenProvider _serviceTokenProvider;

    public DownstreamAuthDelegatingHandler(
        ICallerTokenAccessor callerTokenAccessor,
        IServiceTokenProvider serviceTokenProvider)
    {
        _callerTokenAccessor = callerTokenAccessor;
        _serviceTokenProvider = serviceTokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = _callerTokenAccessor.TryGetToken(out var callerToken)
            ? callerToken
            : await _serviceTokenProvider.GetTokenAsync(cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
