using System.Security.Cryptography;
using System.Text;
using BuildingBlocks.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sales.Application.Common.Interfaces;
using Sales.Api.Controllers;
using Sales.Api.Models.Requests;
using Sales.Api.Models.Responses;
using Sales.Infrastructure;

namespace Sales.Api.Tests;

public sealed class AuthControllerRefreshTests
{
    [Fact]
    public async Task Valid_refresh_token_returns_new_token_pair_and_rotates()
    {
        await using var fixture = await AuthFixture.CreateAsync();
        var login = await fixture.LoginAsync();

        var result = await fixture.Controller.Refresh(new RefreshRequest { RefreshToken = login.RefreshToken }, CancellationToken.None);

        var token = AssertTokenResponse(result);
        Assert.NotEqual(login.AccessToken, token.AccessToken);
        Assert.NotEqual(login.RefreshToken, token.RefreshToken);

        var oldToken = await fixture.Db.RefreshTokens.SingleAsync(x => x.TokenHash == Hash(login.RefreshToken));
        var newToken = await fixture.Db.RefreshTokens.SingleAsync(x => x.TokenHash == Hash(token.RefreshToken));
        Assert.NotNull(oldToken.RevokedAt);
        Assert.Equal(newToken.Id, oldToken.ReplacedByTokenId);
        Assert.Null(newToken.RevokedAt);
        Assert.NotEqual(login.RefreshToken, oldToken.TokenHash);
    }

    [Fact]
    public async Task Expired_refresh_token_is_rejected()
    {
        await using var fixture = await AuthFixture.CreateAsync();
        var login = await fixture.LoginAsync();
        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddDays(8);

        var result = await fixture.Controller.Refresh(new RefreshRequest { RefreshToken = login.RefreshToken }, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Revoked_refresh_token_is_rejected()
    {
        await using var fixture = await AuthFixture.CreateAsync();
        var login = await fixture.LoginAsync();
        var stored = await fixture.Db.RefreshTokens.SingleAsync(x => x.TokenHash == Hash(login.RefreshToken));
        stored.RevokedAt = fixture.Clock.UtcNow;
        await fixture.Db.SaveChangesAsync();

        var result = await fixture.Controller.Refresh(new RefreshRequest { RefreshToken = login.RefreshToken }, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Reused_refresh_token_is_detected_and_revokes_active_tokens()
    {
        await using var fixture = await AuthFixture.CreateAsync();
        var login = await fixture.LoginAsync();
        var rotated = AssertTokenResponse(await fixture.Controller.Refresh(
            new RefreshRequest { RefreshToken = login.RefreshToken },
            CancellationToken.None));

        var result = await fixture.Controller.Refresh(new RefreshRequest { RefreshToken = login.RefreshToken }, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        var replacement = await fixture.Db.RefreshTokens.SingleAsync(x => x.TokenHash == Hash(rotated.RefreshToken));
        Assert.NotNull(replacement.RevokedAt);
    }

    [Fact]
    public async Task Old_refresh_token_cannot_be_reused_after_rotation()
    {
        await using var fixture = await AuthFixture.CreateAsync();
        var login = await fixture.LoginAsync();
        AssertTokenResponse(await fixture.Controller.Refresh(
            new RefreshRequest { RefreshToken = login.RefreshToken },
            CancellationToken.None));

        var result = await fixture.Controller.Refresh(new RefreshRequest { RefreshToken = login.RefreshToken }, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    private static TokenResponse AssertTokenResponse(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = ok.Value;
        var data = envelope?.GetType().GetProperty("Data")?.GetValue(envelope);
        return Assert.IsType<TokenResponse>(data);
    }

    private static string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private sealed class AuthFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly ApplicationUser _user;

        private AuthFixture(ServiceProvider provider, SalesDbContext db, UserManager<ApplicationUser> users, AuthController controller, MutableClock clock, ApplicationUser user)
        {
            _provider = provider;
            Db = db;
            Users = users;
            Controller = controller;
            Clock = clock;
            _user = user;
        }

        public SalesDbContext Db { get; }

        public UserManager<ApplicationUser> Users { get; }

        public AuthController Controller { get; }

        public MutableClock Clock { get; }

        public static async Task<AuthFixture> CreateAsync()
        {
            var clock = new MutableClock(new DateTimeOffset(2026, 7, 23, 0, 0, 0, TimeSpan.Zero));
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IClock>(clock);
            services.AddSingleton<IExecutionContext, TestExecutionContext>();
            services.AddDbContext<SalesDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
            services.AddIdentityCore<ApplicationUser>(options =>
                {
                    options.Password.RequiredLength = 8;
                    options.User.RequireUniqueEmail = false;
                })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<SalesDbContext>();

            var provider = services.BuildServiceProvider();
            var db = provider.GetRequiredService<SalesDbContext>();
            await db.Database.EnsureCreatedAsync();

            var users = provider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "admin", Email = "admin@example.local", EmailConfirmed = true };
            var createResult = await users.CreateAsync(user, "Admin123!");
            Assert.True(createResult.Succeeded, string.Join(", ", createResult.Errors.Select(error => error.Description)));

            var controller = new AuthController(users, db, Configuration(), clock)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        Connection =
                        {
                            RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1")
                        }
                    }
                }
            };

            return new AuthFixture(provider, db, users, controller, clock, user);
        }

        public async Task<TokenResponse> LoginAsync()
        {
            return AssertTokenResponse(await Controller.Login(
                new LoginRequest { UserName = _user.UserName!, Password = "Admin123!" },
                CancellationToken.None));
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _provider.DisposeAsync();
        }

        private static IConfiguration Configuration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "unit-test-development-key-32-characters-long",
                    ["Jwt:Issuer"] = "sales-api",
                    ["Jwt:Audience"] = "sales-clients"
                })
                .Build();
        }
    }

    private sealed class MutableClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }

    private sealed class TestExecutionContext : IExecutionContext
    {
        public string Actor => "auth-tests";

        public Guid CorrelationId { get; } = Guid.NewGuid();
    }
}
