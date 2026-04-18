using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Features.Wells.Queries.PreviewUwi;

public sealed record PreviewUwiQuery(
    string CodigoDaneDpto,
    string CodigoDaneMpio,
    string Denominacion,
    int Consecutivo,
    string? ClusterNombre,
    int ClusterNumero,
    string TipoAngulo,
    string TipoTrayectoria,
    int TrayectoriaConsecutivo,
    string TipoObjetivo,
    string TipoTerminacion,
    bool IsAnh,
    Guid? ExcludeWellId
) : IRequest<Result<UwiPreviewDto>>;
