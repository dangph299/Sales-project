using Dashboard.Bff.Auth;
using Microsoft.AspNetCore.Http;

namespace Dashboard.Bff.Tests;

/// <summary>
/// Exercises <see cref="CallerTokenAccessor"/>'s real header-parsing logic directly (no fakes):
/// <c>Bearer </c> prefix matching, case-insensitivity, empty/missing-header handling, and the
/// no-<see cref="HttpContext"/> background-job scenario.
/// </summary>
public sealed class CallerTokenAccessorTests
{
    private static CallerTokenAccessor CreateAccessor(HttpContext? httpContext)
    {
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        return new CallerTokenAccessor(httpContextAccessor);
    }

    [Fact]
    public void TryGetToken_with_bearer_header_returns_true_and_token()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer caller-jwt";
        var accessor = CreateAccessor(context);

        var result = accessor.TryGetToken(out var token);

        Assert.True(result);
        Assert.Equal("caller-jwt", token);
    }

    [Fact]
    public void TryGetToken_with_lowercase_bearer_scheme_returns_true_and_token()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "bearer caller-jwt";
        var accessor = CreateAccessor(context);

        var result = accessor.TryGetToken(out var token);

        Assert.True(result);
        Assert.Equal("caller-jwt", token);
    }

    [Fact]
    public void TryGetToken_with_uppercase_bearer_scheme_returns_true_and_token()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "BEARER caller-jwt";
        var accessor = CreateAccessor(context);

        var result = accessor.TryGetToken(out var token);

        Assert.True(result);
        Assert.Equal("caller-jwt", token);
    }

    [Fact]
    public void TryGetToken_with_no_authorization_header_returns_false()
    {
        var context = new DefaultHttpContext();
        var accessor = CreateAccessor(context);

        var result = accessor.TryGetToken(out var token);

        Assert.False(result);
        Assert.Equal(string.Empty, token);
    }

    [Fact]
    public void TryGetToken_with_empty_token_after_bearer_returns_false()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer ";
        var accessor = CreateAccessor(context);

        var result = accessor.TryGetToken(out var token);

        Assert.False(result);
        Assert.Equal(string.Empty, token);
    }

    [Fact]
    public void TryGetToken_with_whitespace_only_token_after_bearer_returns_false()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer    ";
        var accessor = CreateAccessor(context);

        var result = accessor.TryGetToken(out var token);

        Assert.False(result);
        Assert.Equal(string.Empty, token);
    }

    [Fact]
    public void TryGetToken_with_no_http_context_returns_false_and_does_not_throw()
    {
        var accessor = CreateAccessor(httpContext: null);

        var result = accessor.TryGetToken(out var token);

        Assert.False(result);
        Assert.Equal(string.Empty, token);
    }

    [Fact]
    public void TryGetToken_with_non_bearer_scheme_returns_false()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Basic dXNlcjpwYXNz";
        var accessor = CreateAccessor(context);

        var result = accessor.TryGetToken(out var token);

        Assert.False(result);
        Assert.Equal(string.Empty, token);
    }
}
