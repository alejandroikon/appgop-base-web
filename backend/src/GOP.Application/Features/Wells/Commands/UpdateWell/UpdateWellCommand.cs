using GOP.Application.Features.Wells.Queries.GetWellById;
using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Features.Wells.Commands.UpdateWell;

/// <summary>
/// Actualizar un pozo existente. action=SAVE: guardar cambios.
/// action=FINALIZE: finalizar un borrador (genera UWI).
/// Bloqueado si Forma 101 radicada (RN-40).
/// </summary>
public sealed record UpdateWellCommand(
    Guid WellId,
    string Action,                      // SAVE | FINALIZE
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
