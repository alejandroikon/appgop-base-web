namespace GOP.Application.Features.Wells.Queries.GetWellById;

public sealed record WellDetailDto(
    Guid Id,
    string Operadora,
    int ContratoId,
    string Contrato,
    string TipoContrato,
    string Cuenca,
    int CampoId,
    string Campo,
    string TipoTrayectoria,
    string Clasificacion,
    string Denominacion,
    string Consecutivo,
    string NombrePozo,
    string TipoUbicacion,
    string TipoAngulo,
    string TipoObjetivo,
    string TipoTerminacion,
    string Estado,
    // Ubicación aplanada
    int DepartamentoId,
    string Departamento,
    string CodigoDaneDpto,
    int MunicipioId,
    string Municipio,
    string CodigoDaneMpio,
    int? ClusterId,
    string? Cluster,
    DateTime CreatedAt,
    DateTime? LastModifiedAt
);
