using GOP.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GOP.API.Controllers;

[ApiController]
[Route("api/v1/health")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class HealthController(HealthCheckService healthCheckService) : ControllerBase
{
    [HttpGet(Name = "GetHealth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(cancellationToken);
        var statusCode = report.Status == HealthStatus.Healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
        return StatusCode(statusCode, report.ToApiResponse());
    }
}
