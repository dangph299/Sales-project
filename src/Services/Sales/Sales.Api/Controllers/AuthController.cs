using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sales.Api.Models.Requests;
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

    /// <summary>
    /// Initializes the controller with the services needed to authenticate users and issue tokens.
    /// </summary>
    /// <param name="users">Identity user manager.</param>
    /// <param name="db">Sales persistence context.</param>
    /// <param name="config">Application configuration, used for JWT signing settings.</param>
    public AuthController(UserManager<ApplicationUser> users, SalesDbContext db, IConfiguration config)
    {
        _users = users;
        _db = db;
        _config = config;
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

        return Ok(await IssueTokenAsync(user, ct));
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
        var stored = await _db.RefreshTokens.SingleOrDefaultAsync(x =>
            x.TokenHash == Hash(body.RefreshToken) &&
            x.RevokedAt == null &&
            x.ExpiresAt > DateTimeOffset.UtcNow, ct);

        if (stored is null)
        {
            return Unauthorized();
        }

        stored.RevokedAt = DateTimeOffset.UtcNow;
        var user = await _users.FindByIdAsync(stored.UserId.ToString());
        return user is null ? Unauthorized() : Ok(await IssueTokenAsync(user, ct));
    }

    private async Task<object> IssueTokenAsync(ApplicationUser user, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!)
        };
        var roles = await _users.GetRolesAsync(user);
        claims.AddRange(roles.Select(x => new Claim(ClaimTypes.Role, x)));

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

        return new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(jwt),
            expiresIn = 1800,
            refreshToken = refresh
        };
    }

    private static string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
