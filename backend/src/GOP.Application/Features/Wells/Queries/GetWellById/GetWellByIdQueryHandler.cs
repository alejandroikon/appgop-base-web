using GOP.Application.Common.Interfaces;
using GOP.Domain.Common;
using GOP.Domain.Errors;
using GOP.Domain.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GOP.Application.Features.Wells.Queries.GetWellById;

internal sealed class GetWellByIdQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser
) : IRequestHandler<GetWellByIdQuery, Result<WellDetailDto>>
{
    public async Task<Result<WellDetailDto>> Handle(
        GetWellByIdQuery request, CancellationToken cancellationToken)
    {
        var isAdminOrAuditor = currentUser.IsInRole("ADMIN") || currentUser.IsInRole("AUDITOR");

        var wellQuery = dbContext.Wells.AsNoTracking();
        if (isAdminOrAuditor)
            wellQuery = wellQuery.IgnoreQueryFilters();

        var well = await wellQuery
            .FirstOrDefaultAsync(w => w.Id == request.WellId, cancellationToken);

        if (well is null)
            return Result.Failure<WellDetailDto>(DomainErrors.Well.NotFoundById(request.WellId));

        var dto = new WellDetailDto(
            Id: well.Id,
            Operadora: well.Operadora,
            ContratoId: well.ContratoId,
            Contrato: well.Contrato,
            TipoContrato: well.TipoContrato,
            Cuenca: well.Cuenca,
            CampoId: well.CampoId,
            Campo: well.Campo,
            Denominacion: well.Denominacion,
            Consecutivo: well.Consecutivo,
            NombrePozo: well.NombrePozo,
            TipoTrayectoria: well.TipoTrayectoria.ToString(),
            Clasificacion: well.Clasificacion.ToString().ToUpperInvariant(),
            SubClasificacion: well.SubClasificacion?.ToString(),
            TipoUbicacion: well.TipoUbicacion.ToString().ToUpperInvariant(),
            TipoAngulo: well.TipoAngulo.ToString(),
            TipoObjetivo: well.TipoObjetivo.ToString(),
            TipoTerminacion: well.TipoTerminacion.ToString(),
            Estado: well.Estado.ToString().ToUpperInvariant(),
            Uwi: well.Uwi,
            Forma101Radicada: well.Forma101Radicada,
            DepartamentoId: well.DepartamentoId,
            Departamento: well.Departamento,
            CodigoDaneDpto: well.CodigoDaneDpto,
            MunicipioId: well.MunicipioId,
            Municipio: well.Municipio,
            CodigoDaneMpio: well.CodigoDaneMpio,
            ClusterId: well.ClusterId,
            Cluster: well.Cluster,
            CreatedAt: well.CreatedAt,
            LastModifiedAt: well.LastModifiedAt
        );

        return Result.Success(dto);
    }
}
