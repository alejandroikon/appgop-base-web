using GOP.Application.Features.Wells.Queries.GetWellById;
using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Features.Wells.Commands.CreateWell;

/// <summary>
/// Crear un pozo nuevo. action=DRAFT: borrador parcial sin UWI.
/// action=FINALIZE: registro completo con generación de UWI.
/// </summary>
public sealed record CreateWellCommand(
    string Action,                      // DRAFT | FINALIZE
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
) : IRequest<Result<WellDetailDto>>;
