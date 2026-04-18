using GOP.Application.Common.Interfaces;
using GOP.Domain.Common;
using GOP.Domain.Enums;
using GOP.Domain.Interfaces.Repositories;
using GOP.Domain.ValueObjects;
using MediatR;

namespace GOP.Application.Features.Wells.Queries.PreviewUwi;

internal sealed class PreviewUwiQueryHandler(
    IWellRepository wellRepository
) : IRequestHandler<PreviewUwiQuery, Result<UwiPreviewDto>>
{
    public async Task<Result<UwiPreviewDto>> Handle(
        PreviewUwiQuery request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TipoAngulo>(request.TipoAngulo, ignoreCase: true, out var tipoAngulo))
            return Result.Failure<UwiPreviewDto>(new Error("PreviewUwi.InvalidAngulo",
                $"Tipo de ángulo inválido: '{request.TipoAngulo}'."));

        if (!Enum.TryParse<TipoTrayectoria>(request.TipoTrayectoria, ignoreCase: true, out var tipoTrayectoria))
            return Result.Failure<UwiPreviewDto>(new Error("PreviewUwi.InvalidTrayectoria",
                $"Tipo de trayectoria inválido: '{request.TipoTrayectoria}'."));

        if (!Enum.TryParse<TipoObjetivo>(request.TipoObjetivo, ignoreCase: true, out var tipoObjetivo))
            return Result.Failure<UwiPreviewDto>(new Error("PreviewUwi.InvalidObjetivo",
                $"Tipo de objetivo inválido: '{request.TipoObjetivo}'."));

        if (!Enum.TryParse<TipoTerminacion>(request.TipoTerminacion, ignoreCase: true, out var tipoTerminacion))
            return Result.Failure<UwiPreviewDto>(new Error("PreviewUwi.InvalidTerminacion",
                $"Tipo de terminación inválido: '{request.TipoTerminacion}'."));

        var uwiResult = Uwi.Generate(
            codigoDaneDpto: request.CodigoDaneDpto,
            codigoDaneMpio: request.CodigoDaneMpio,
            denominacion: request.Denominacion,
            consecutivo: request.Consecutivo,
            clusterNombre: request.ClusterNombre,
            clusterNumero: request.ClusterNumero,
            tipoAngulo: tipoAngulo,
            tipoTrayectoria: tipoTrayectoria,
            trayectoriaConsecutivo: request.TrayectoriaConsecutivo,
            tipoObjetivo: tipoObjetivo,
            tipoTerminacion: tipoTerminacion,
            isAnh: request.IsAnh);

        if (uwiResult.IsFailure)
            return Result.Failure<UwiPreviewDto>(uwiResult.Error);

        var uwi = uwiResult.Value;

        var isUnique = !await wellRepository.ExistsByUwiAsync(
            uwi.Value, request.ExcludeWellId, cancellationToken);

        var dto = new UwiPreviewDto(
            Uwi: uwi.Value,
            IsUnique: isUnique,
            Components: new UwiComponentsDto(
                DptoCode: uwi.DptoCode,
                MpioCode: uwi.MpioCode,
                Sigla: uwi.Sigla,
                Numero: uwi.Numero,
                ClusterCode: uwi.ClusterCode,
                AnguloCode: uwi.AnguloCode,
                TrayectoriaCode: uwi.TrayectoriaCode,
                ObjetivoCode: uwi.ObjetivoCode,
                TerminacionCode: uwi.TerminacionCode
            )
        );

        return Result.Success(dto);
    }
}
