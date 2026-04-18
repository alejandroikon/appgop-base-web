namespace GOP.API.Contracts;

/// <summary>
/// DTO de body para PUT /api/v1/wells/{id}. V2.0 — acción SAVE|FINALIZE.
/// </summary>
public sealed record UpdateWellRequest(
    string Action,          // SAVE | FINALIZE
    int? ContratoId,
    int? CampoId,
    string? Denominacion,
    int? Consecutivo,
    string? TipoTrayectoria,
    string? Clasificacion,
    string? SubClasificacion,
    string? TipoUbicacion,
    string? TipoAngulo,
    string? TipoObjetivo,
    string? TipoTerminacion,
    int? DepartamentoId,
    int? MunicipioId,
    int? ClusterId,
    int ClusterNumero = 0,
    int TrayectoriaConsecutivo = 1
);
