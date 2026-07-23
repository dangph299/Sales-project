using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BuildingBlocks.Application;
using BuildingBlocks.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sales.Api.Models.Requests;
using Sales.Api.Models.Responses;
using Sales.Infrastructure;

namespace Sales.Api.Controllers;

/// <summary>
/// Unauthenticated HTTP API for issuing and refreshing JWT access tokens. Uses ASP.NET Core
/// Identity and <see cref="SalesDbContext"/> directly, outside of the CQRS pipeline, since
/// authentication is not a Sales business use case.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SalesDbContext _db;
    private readonly IConfiguration _config;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes the controller with the services needed to authenticate users and issue tokens.
    /// </summary>
    /// <param name="users">Identity user manager.</param>
    /// <param name="db">Sales persistence context.</param>
    /// <param name="config">Application configuration, used for JWT signing settings.</param>
    /// <param name="clock">Clock used to stamp token expiration and revocation times.</param>
    public AuthController(UserManager<ApplicationUser> users, SalesDbContext db, IConfiguration config, IClock clock)
    {
        _users = users;
        _db = db;
        _config = config;
        _clock = clock;
    }

    /// <summary>
    /// Authenticates a user by username and password and issues a new access/refresh token pair.
    /// </summary>
    /// <param name="body">Username and password to authenticate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the issued tokens, or <c>401 Unauthorized</c> if the credentials are invalid.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest body, CancellationToken ct)
    {
        var user = await _users.FindByNameAsync(body.UserName);
        if (user is null || !await IsActiveAsync(user) || !await _users.CheckPasswordAsync(user, body.Password))
        {
            return Unauthorized();
        }

        var token = await IssueTokenAsync(user, ct);
        return this.ToOkResponse(token.Response);
    }

    /// <summary>
    /// Exchanges a valid, unrevoked refresh token for a new access/refresh token pair, revoking the
    /// used refresh token.
    /// </summary>
    /// <param name="body">Refresh token to exchange.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the newly issued tokens, or <c>401 Unauthorized</c> if the refresh token is missing, expired, revoked, or does not match a known user.</returns>
    [HttpPost("refresh")]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest body, CancellationToken ct)
    {
        var tokenHash = Hash(body.RefreshToken);
        var stored = await _db.RefreshTokens.SingleOrDefaultAsync(
            refreshToken => refreshToken.TokenHash == tokenHash,
            ct);

        if (stored is null)
        {
            return Unauthorized();
        }

        if (stored.RevokedAt is not null)
        {
            await RevokeActiveRefreshTokensAsync(stored.UserId, ct);
            return Unauthorized();
        }

        var now = _clock.UtcNow;
        if (stored.ExpiresAt <= now)
        {
            return Unauthorized();
        }

        var user = await _users.FindByIdAsync(stored.UserId.ToString());
        if (user is null || !await IsActiveAsync(user))
        {
            return Unauthorized();
        }

        stored.RevokedAt = now;
        var token = await IssueTokenAsync(user, ct, save: false);
        stored.ReplacedByTokenId = token.RefreshTokenId;
        await _db.SaveChangesAsync(ct);
        return this.ToOkResponse(token.Response);
    }

    private async Task<IssuedToken> IssueTokenAsync(ApplicationUser user, CancellationToken ct, bool save = true)
    {
        var now = _clock.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!)
        };
        var roles = await _users.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "development-only-key-change-me-32-chars")),
            SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            _config["Jwt:Issuer"] ?? "sales-api",
            _config["Jwt:Audience"] ?? "sales-clients",
            claims,
            now.UtcDateTime,
            now.AddMinutes(30).UtcDateTime,
            credentials);

        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var refreshTokenId = Guid.NewGuid();
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = refreshTokenId,
            UserId = user.Id,
            TokenHash = Hash(refresh),
            ExpiresAt = now.AddDays(7),
            CreatedAt = now,
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        if (save)
        {
            await _db.SaveChangesAsync(ct);
        }

        return new IssuedToken(
            refreshTokenId,
            new TokenResponse(
                new JwtSecurityTokenHandler().WriteToken(jwt),
                1800,
                refresh));
    }

    private async Task<bool> IsActiveAsync(ApplicationUser user)
    {
        return !await _users.IsLockedOutAsync(user);
    }

    private async Task RevokeActiveRefreshTokensAsync(Guid userId, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var tokens = await _db.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in tokens.Where(token => token.ExpiresAt > now))
        {
            token.RevokedAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }

    private static string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private sealed record IssuedToken(Guid RefreshTokenId, TokenResponse Response);
}
