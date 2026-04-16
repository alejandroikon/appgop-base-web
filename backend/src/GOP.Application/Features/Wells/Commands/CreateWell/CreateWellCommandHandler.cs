using GOP.Application.Common.Interfaces;
using GOP.Application.Features.Wells.Queries.GetWellById;
using GOP.Domain.Common;
using GOP.Domain.Entities;
using GOP.Domain.Enums;
using GOP.Domain.Errors;
using GOP.Domain.Interfaces;
using GOP.Domain.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GOP.Application.Features.Wells.Commands.CreateWell;

internal sealed class CreateWellCommandHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<CreateWellCommand, Result<WellDetailDto>>
{
    public async Task<Result<WellDetailDto>> Handle(
        CreateWellCommand request, CancellationToken cancellationToken)
    {
        // 1. Buscar contrato y extraer datos desnormalizados
        var contrato = await dbContext.Contratos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ContratoId, cancellationToken);

        if (contrato is null)
            return Result.Failure<WellDetailDto>(DomainErrors.Well.CampoNotBelongsToContrato);

        // 2. Validar que el campo pertenece al contrato
        var campo = await dbContext.Campos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CampoId && c.ContratoId == request.ContratoId, cancellationToken);

        if (campo is null)
            return Result.Failure<WellDetailDto>(DomainErrors.Well.CampoNotBelongsToContrato);

        // 3. Buscar departamento
        var departamento = await dbContext.Departamentos
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DepartamentoId, cancellationToken);

        if (departamento is null)
            return Result.Failure<WellDetailDto>(DomainErrors.Well.MunicipioNotBelongsToDepartamento);

        // 4. Validar que el municipio pertenece al departamento
        var municipio = await dbContext.Municipios
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.MunicipioId && m.DepartamentoId == request.DepartamentoId, cancellationToken);

        if (municipio is null)
            return Result.Failure<WellDetailDto>(DomainErrors.Well.MunicipioNotBelongsToDepartamento);

        // 5. Resolver cluster (opcional)
        string? clusterNombre = null;
        if (request.ClusterId.HasValue)
        {
            var cluster = await dbContext.Clusters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.ClusterId.Value, cancellationToken);
            clusterNombre = cluster?.Nombre;
        }

        // 6. Parsear enums
        var tipoTrayectoria = Enum.Parse<TipoTrayectoria>(request.TipoTrayectoria, ignoreCase: true);
        var clasificacion = Enum.Parse<Clasificacion>(request.Clasificacion, ignoreCase: true);
        var tipoUbicacion = Enum.Parse<TipoUbicacion>(request.TipoUbicacion, ignoreCase: true);
        var tipoAngulo = Enum.Parse<TipoAngulo>(request.TipoAngulo, ignoreCase: true);
        var tipoObjetivo = Enum.Parse<TipoObjetivo>(request.TipoObjetivo, ignoreCase: true);
        var tipoTerminacion = Enum.Parse<TipoTerminacion>(request.TipoTerminacion, ignoreCase: true);

        // 7. Construir owned entity de ubicación
        var location = new WellLocation
        {
            DepartamentoId = request.DepartamentoId,
            MunicipioId = request.MunicipioId,
            ClusterId = request.ClusterId,
            CodigoDaneDpto = departamento.CodigoDane,
            CodigoDaneMpio = municipio.CodigoDane
        };

        // 8. Crear la entidad Well via factory method
        var well = Well.Create(
            operadora: currentUser.TenantName,
            tenantId: currentUser.TenantId,
            contratoId: request.ContratoId,
            tipoContrato: contrato.Tipo,
            cuenca: contrato.Cuenca,
            campoId: request.CampoId,
            tipoTrayectoria: tipoTrayectoria,
            clasificacion: clasificacion,
            denominacion: request.Denominacion,
            consecutivo: request.Consecutivo,
            tipoUbicacion: tipoUbicacion,
            tipoAngulo: tipoAngulo,
            tipoObjetivo: tipoObjetivo,
            tipoTerminacion: tipoTerminacion,
            location: location);

        // 9. Persistir
        dbContext.Wells.Add(well);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 10. Construir DTO de respuesta
        var dto = BuildDetailDto(well, contrato.Nombre, campo.Nombre,
            departamento.Nombre, municipio.Nombre, clusterNombre);

        return Result.Success(dto);
    }

    private static WellDetailDto BuildDetailDto(
        Well well, string contratoNombre, string campoNombre,
        string departamentoNombre, string municipioNombre, string? clusterNombre) =>
        new(
            Id: well.Id,
            Operadora: well.Operadora,
            ContratoId: well.ContratoId,
            Contrato: contratoNombre,
            TipoContrato: well.TipoContrato,
            Cuenca: well.Cuenca,
            CampoId: well.CampoId,
            Campo: campoNombre,
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
            Departamento: departamentoNombre,
            CodigoDaneDpto: well.Location.CodigoDaneDpto,
            MunicipioId: well.Location.MunicipioId,
            Municipio: municipioNombre,
            CodigoDaneMpio: well.Location.CodigoDaneMpio,
            ClusterId: well.Location.ClusterId,
            Cluster: clusterNombre,
            CreatedAt: well.CreatedAt,
            LastModifiedAt: well.LastModifiedAt
        );
}
