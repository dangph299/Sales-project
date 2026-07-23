using BuildingBlocks.Web.Extensions;
using Dashboard.Bff.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Bff.Controllers;

/// <summary>
/// HTTP API aggregating dashboard data for the Sales web client.
/// </summary>
[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    /// <summary>
    /// Loads the current dashboard snapshot.
    /// </summary>
    /// <returns><c>200 OK</c> with the dashboard snapshot.</returns>
    [HttpGet]
    public IActionResult Get()
    {
        var snapshot = new DashboardSnapshot(
            new DashboardMetrics(0, 0, 0, 0, 0, 0),
            new InventorySummaryDto(0, 0, 0, 0, 0, 5),
            [],
            [],
            DateTimeOffset.UtcNow);

        return this.ToOkResponse(snapshot);
    }
}
