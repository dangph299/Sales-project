using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Sales.Api.Controllers;

namespace Sales.Api.Tests;

public sealed class CategoriesControllerAuthorizationTests
{
    [Fact]
    public void Listing_categories_is_open_to_any_authenticated_user()
    {
        // ASP.NET Core combines controller- and action-level [Authorize]: every one of them must pass.
        // A role-free action attribute therefore cannot relax a role-bearing controller attribute, so
        // the controller policy itself has to stay role-free for the product form's category dropdown
        // to be readable by non-Admin users.
        Assert.Empty(GetAuthorizeAttributes(nameof(CategoriesController.List)));

        var controllerPolicy = Assert.Single(ControllerAuthorizeAttributes());

        Assert.Null(controllerPolicy.Roles);
    }

    [Theory]
    [InlineData(nameof(CategoriesController.Create))]
    [InlineData(nameof(CategoriesController.Update))]
    [InlineData(nameof(CategoriesController.Delete))]
    public void Mutating_categories_stays_restricted_to_administrators(string actionName)
    {
        var authorize = Assert.Single(GetAuthorizeAttributes(actionName));

        Assert.Equal("Admin", authorize.Roles);
    }

    private static AuthorizeAttribute[] ControllerAuthorizeAttributes()
    {
        return typeof(CategoriesController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .OfType<AuthorizeAttribute>()
            .ToArray();
    }

    private static AuthorizeAttribute[] GetAuthorizeAttributes(string actionName)
    {
        var action = typeof(CategoriesController).GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(action);

        return action.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .OfType<AuthorizeAttribute>()
            .ToArray();
    }
}
