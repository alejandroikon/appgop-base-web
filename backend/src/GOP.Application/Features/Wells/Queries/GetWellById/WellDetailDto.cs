namespace GOP.Application.Features.Wells.Queries.GetWellById;

public sealed record WellDetailDto(
    Guid Id,
    string Operadora,
    // Contrato
    int? ContratoId,
    string? Contrato,
    string? TipoContrato,
    string? Cuenca,
    // Campo
    int? CampoId,
    string? Campo,
    // Datos Técnicos
    string? Denominacion,
    int? Consecutivo,
    string? NombrePozo,
    string? TipoTrayectoria,
    string? Clasificacion,
    string? SubClasificacion,
    string? TipoUbicacion,
    string? TipoAngulo,
    string? TipoObjetivo,
    string? TipoTerminacion,
    // Estado
    string Estado,
    string? Uwi,
    bool Forma101Radicada,
    // Ubicación
    int? DepartamentoId,
    string? Departamento,
    string? CodigoDaneDpto,
    int? MunicipioId,
    string? Municipio,
    string? CodigoDaneMpio,
    int? ClusterId,
    string? Cluster,
    // Auditoría
    DateTime CreatedAt,
    DateTime? LastModifiedAt
);
