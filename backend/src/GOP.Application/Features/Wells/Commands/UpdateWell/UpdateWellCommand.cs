using GOP.Application.Features.Wells.Queries.GetWellById;
using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Features.Wells.Commands.UpdateWell;

public sealed record UpdateWellCommand(
    Guid WellId,
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
) : IRequest<Result<WellDetailDto>>;
