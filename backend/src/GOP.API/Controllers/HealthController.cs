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
    /// <summary>GET /api/v1/health — Liveness probe (siempre 200 si el proceso está vivo)</summary>
    [HttpGet(Name = "GetHealth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(cancellationToken);
        var statusCode = report.Status == HealthStatus.Healthy
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable;
        return StatusCode(statusCode, report.ToApiResponse());
    }

    /// <summary>
    /// GET /health/ready — Readiness probe: valida conexión DB con CanConnectAsync.
    /// Usado por App Service Health Check y por el smoke test de CI/CD.
    /// CA-01, RN-INFRA-04
    /// </summary>
    [HttpGet("/health/ready", Name = "GetHealthReady")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealthReady(CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(
            predicate: hc => hc.Tags.Contains("db"),
            cancellationToken: cancellationToken);

        var statusCode = report.Status == HealthStatus.Healthy
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable;

        return StatusCode(statusCode, new
        {
            status = report.Status == HealthStatus.Healthy ? "Healthy" : "Unhealthy",
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        });
    }

    /// <summary>GET /health — Root health (alias para compatibilidad con Azure App Service)</summary>
    [HttpGet("/health", Name = "GetHealthRoot")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealthRoot() => Ok(new { status = "Healthy" });
}
