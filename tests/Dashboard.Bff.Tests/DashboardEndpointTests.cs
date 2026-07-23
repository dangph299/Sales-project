using BuildingBlocks.Web.Models;
using Dashboard.Bff.Contracts;
using Dashboard.Bff.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Bff.Tests;

public sealed class DashboardEndpointTests
{
    [Fact]
    public void Get_returns_empty_snapshot()
    {
        var controller = new DashboardController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<DashboardSnapshot>>(okResult.Value);
        var snapshot = response.Data;

        Assert.NotNull(snapshot);
        Assert.Empty(snapshot!.RecentOrders);
        Assert.Empty(snapshot.OrderChart);
        Assert.Equal(5, snapshot.Inventory.LowStockThreshold);
    }

    [Fact]
    public void DashboardController_requires_authorization()
    {
        var attribute = Assert.Single(
            typeof(DashboardController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
                .OfType<AuthorizeAttribute>());

        Assert.NotNull(attribute);
    }
}
