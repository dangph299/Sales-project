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
        if (user is null || !await _users.CheckPasswordAsync(user, body.Password))
        {
            return Unauthorized();
        }

        var token = await IssueTokenAsync(user, ct);
        return this.ToOkResponse(token);
    }

    /// <summary>
    /// Exchanges a valid, unrevoked refresh token for a new access/refresh token pair, revoking the
    /// used refresh token.
    /// </summary>
    /// <param name="body">Refresh token to exchange.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the newly issued tokens, or <c>401 Unauthorized</c> if the refresh token is missing, expired, revoked, or does not match a known user.</returns>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest body, CancellationToken ct)
    {
        var tokenHash = Hash(body.RefreshToken);
        var stored = await (
            from refreshToken in _db.RefreshTokens
            where refreshToken.TokenHash == tokenHash &&
                  refreshToken.RevokedAt == null &&
                  refreshToken.ExpiresAt > _clock.UtcNow
            select refreshToken).SingleOrDefaultAsync(ct);

        if (stored is null)
        {
            return Unauthorized();
        }

        stored.RevokedAt = _clock.UtcNow;
        var user = await _users.FindByIdAsync(stored.UserId.ToString());
        if (user is null)
        {
            return Unauthorized();
        }

        var token = await IssueTokenAsync(user, ct);
        return this.ToOkResponse(token);
    }

    private async Task<TokenResponse> IssueTokenAsync(ApplicationUser user, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var claims = new List<Claim>
        {
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
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = Hash(refresh),
            ExpiresAt = now.AddDays(7)
        });
        await _db.SaveChangesAsync(ct);

        return new TokenResponse(
            new JwtSecurityTokenHandler().WriteToken(jwt),
            1800,
            refresh);
    }

    private static string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
