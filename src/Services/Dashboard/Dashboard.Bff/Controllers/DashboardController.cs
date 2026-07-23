using BuildingBlocks.Web.Extensions;
using Dashboard.Bff.Aggregation;
using Dashboard.Bff.Caching;
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
    private readonly IDashboardSnapshotBuilder _snapshotBuilder;
    private readonly IDashboardSnapshotCache _snapshotCache;

    /// <summary>
    /// Initializes the controller with dashboard snapshot dependencies.
    /// </summary>
    /// <param name="snapshotBuilder">Dashboard snapshot builder.</param>
    /// <param name="snapshotCache">Dashboard snapshot cache.</param>
    public DashboardController(
        IDashboardSnapshotBuilder snapshotBuilder,
        IDashboardSnapshotCache snapshotCache)
    {
        _snapshotBuilder = snapshotBuilder;
        _snapshotCache = snapshotCache;
    }

    /// <summary>
    /// Loads the current dashboard snapshot.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the dashboard snapshot.</returns>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var cached = await _snapshotCache.GetAsync(ct);
        if (cached is not null)
        {
            return this.ToOkResponse(cached);
        }

        var snapshot = await _snapshotBuilder.BuildAsync(ct);
        await _snapshotCache.SetAsync(snapshot, ct);
        return this.ToOkResponse(snapshot);
    }
}
