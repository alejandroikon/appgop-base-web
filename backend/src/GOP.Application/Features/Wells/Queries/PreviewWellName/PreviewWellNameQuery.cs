using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Features.Wells.Queries.PreviewWellName;

public sealed record PreviewWellNameQuery(
    int ContratoId,
    string Denominacion,
    string Consecutivo,
    Guid? ExcludeWellId
) : IRequest<Result<WellNamePreviewDto>>;
