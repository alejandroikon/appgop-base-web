using GOP.API.Contracts;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GOP.API.Extensions;

/// <summary>Extensiones para convertir <see cref="HealthReport"/> al contrato HTTP de la API.</summary>
internal static class HealthCheckExtensions
{
    internal static HealthResponse ToApiResponse(this HealthReport report) =>
        new(
            Status: report.Status.ToString(),
            TotalDuration: report.TotalDuration.ToString(),
            Checks: report.Entries.Select(e => new HealthCheckEntry(
                Name: e.Key,
                Status: e.Value.Status.ToString(),
                Duration: e.Value.Duration.ToString(),
                Description: e.Value.Description))
        );
}
