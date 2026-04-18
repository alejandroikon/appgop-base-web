using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Features.Wells.Queries.PreviewWellName;

public sealed record PreviewWellNameQuery(
    int ContratoId,
    int? CampoId,
    string Denominacion,
    int Consecutivo,
    Guid? ExcludeWellId
) : IRequest<Result<WellNamePreviewDto>>;
