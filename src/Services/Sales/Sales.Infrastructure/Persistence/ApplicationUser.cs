using Microsoft.AspNetCore.Identity;

namespace Sales.Infrastructure;

/// <summary>
/// ASP.NET Core Identity user for Sales authentication, keyed by <see cref="Guid"/>.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid> { }
