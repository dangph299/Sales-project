using Inventory.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Tests;

public sealed class HealthControllerTests
{
    [Fact]
    public void HealthController_is_hidden_from_swagger()
    {
        var attribute = Assert.Single(
            typeof(HealthController).GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), inherit: false)
                .OfType<ApiExplorerSettingsAttribute>());

        Assert.True(attribute.IgnoreApi);
    }
}
