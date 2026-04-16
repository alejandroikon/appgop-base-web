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
        // Admins y Auditores pueden ver pozos de cualquier tenant
        var isAdminOrAuditor = currentUser.IsInRole("ADMIN") || currentUser.IsInRole("AUDITOR");

        var wellQuery = dbContext.Wells.AsNoTracking();

        if (isAdminOrAuditor)
            wellQuery = wellQuery.IgnoreQueryFilters();

        var well = await wellQuery
            .FirstOrDefaultAsync(w => w.Id == request.WellId, cancellationToken);

        if (well is null)
            return Result.Failure<WellDetailDto>(DomainErrors.Well.NotFoundById(request.WellId));

        // Resolver nombres de catálogos
        var contrato = await dbContext.Contratos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == well.ContratoId, cancellationToken);

        var campo = await dbContext.Campos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == well.CampoId, cancellationToken);

        var departamento = await dbContext.Departamentos
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == well.Location.DepartamentoId, cancellationToken);

        var municipio = await dbContext.Municipios
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == well.Location.MunicipioId, cancellationToken);

        string? clusterNombre = null;
        if (well.Location.ClusterId.HasValue)
        {
            var cluster = await dbContext.Clusters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == well.Location.ClusterId.Value, cancellationToken);
            clusterNombre = cluster?.Nombre;
        }

        var dto = new WellDetailDto(
            Id: well.Id,
            Operadora: well.Operadora,
            ContratoId: well.ContratoId,
            Contrato: contrato?.Nombre ?? string.Empty,
            TipoContrato: well.TipoContrato,
            Cuenca: well.Cuenca,
            CampoId: well.CampoId,
            Campo: campo?.Nombre ?? string.Empty,
            TipoTrayectoria: well.TipoTrayectoria.ToString(),
            Clasificacion: well.Clasificacion.ToString(),
            Denominacion: well.Denominacion,
            Consecutivo: well.Consecutivo,
            NombrePozo: well.NombrePozo,
            TipoUbicacion: well.TipoUbicacion.ToString(),
            TipoAngulo: well.TipoAngulo.ToString(),
            TipoObjetivo: well.TipoObjetivo.ToString(),
            TipoTerminacion: well.TipoTerminacion.ToString(),
            Estado: well.Estado.ToString(),
            DepartamentoId: well.Location.DepartamentoId,
            Departamento: departamento?.Nombre ?? string.Empty,
            CodigoDaneDpto: well.Location.CodigoDaneDpto,
            MunicipioId: well.Location.MunicipioId,
            Municipio: municipio?.Nombre ?? string.Empty,
            CodigoDaneMpio: well.Location.CodigoDaneMpio,
            ClusterId: well.Location.ClusterId,
            Cluster: clusterNombre,
            CreatedAt: well.CreatedAt,
            LastModifiedAt: well.LastModifiedAt
        );

        return Result.Success(dto);
    }
}
