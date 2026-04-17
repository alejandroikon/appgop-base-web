namespace GOP.API.Contracts;

/// <summary>Respuesta JSON para GET /api/v1/health.</summary>
public sealed record HealthResponse(
    string Status,
    string TotalDuration,
    IEnumerable<HealthCheckEntry> Checks
);

/// <summary>Entrada individual de un health check.</summary>
public sealed record HealthCheckEntry(
    string Name,
    string Status,
    string Duration,
    string? Description
);
