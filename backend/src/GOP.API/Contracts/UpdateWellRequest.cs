namespace GOP.API.Contracts;

/// <summary>
/// DTO de body para PUT /api/v1/wells/{id}.
/// Desacopla el Id del path del body y mantiene la regla "un archivo = una clase".
/// </summary>
public sealed record UpdateWellRequest(
    int ContratoId,
    int CampoId,
    string TipoTrayectoria,
    string Clasificacion,
    string Denominacion,
    string Consecutivo,
    string TipoUbicacion,
    string TipoAngulo,
    string TipoObjetivo,
    string TipoTerminacion,
    int DepartamentoId,
    int MunicipioId,
    int? ClusterId
);
