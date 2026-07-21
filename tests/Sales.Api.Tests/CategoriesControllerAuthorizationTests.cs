using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Sales.Api.Controllers;

namespace Sales.Api.Tests;

public sealed class CategoriesControllerAuthorizationTests
{
    [Fact]
    public void Listing_categories_is_open_to_any_authenticated_user()
    {
        var authorize = Assert.Single(GetAuthorizeAttributes(nameof(CategoriesController.List)));

        Assert.Null(authorize.Roles);
    }

    [Theory]
    [InlineData(nameof(CategoriesController.Create))]
    [InlineData(nameof(CategoriesController.Update))]
    public void Mutating_categories_stays_restricted_to_administrators(string actionName)
    {
        Assert.Empty(GetAuthorizeAttributes(actionName));

        var controllerPolicy = Assert.Single(
            typeof(CategoriesController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
                .OfType<AuthorizeAttribute>());

        Assert.Equal("Admin", controllerPolicy.Roles);
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
