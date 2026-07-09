using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sales.Infrastructure;

namespace Sales.Api.Extensions;

/// <summary>
/// Development bootstrap that applies pending migrations and seeds baseline Identity roles and an
/// admin user, so a fresh environment is usable without manual setup.
/// </summary>
public static class IdentitySeeder
{
    /// <summary>
    /// Applies pending database migrations, ensures the <c>Admin</c>/<c>Sales</c>/<c>Warehouse</c>
    /// roles exist, and creates a default <c>admin</c> user if none exists.
    /// </summary>
    /// <param name="services">
    /// The application's root service provider, used to resolve a scoped set of Identity services.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    public static async Task SeedIdentityAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
        await db.Database.MigrateAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in new[] { "Admin", "Sales", "Warehouse" })
            if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole<Guid>(role));
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (await users.FindByNameAsync("admin") is null)
        {
            var admin = new ApplicationUser { Id = Guid.NewGuid(), UserName = "admin", Email = "admin@example.local", EmailConfirmed = true };
            await users.CreateAsync(admin, "Admin123!");
            await users.AddToRoleAsync(admin, "Admin");
        }
    }
}
