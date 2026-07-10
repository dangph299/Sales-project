using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sales.Api.Controllers;

/// <summary>
/// Unauthenticated liveness endpoint used by container orchestration/health checks.
/// </summary>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[AllowAnonymous]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// Reports that the API is up and able to handle requests.
    /// </summary>
    /// <returns>
    /// <c>200 OK</c> with a simple status payload.
    /// </returns>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy" });
    }
}
