using GOP.Application.Common.Interfaces;
using GOP.Application.Features.Wells.Queries.GetWellById;
using GOP.Domain.Common;
using GOP.Domain.Entities;
using GOP.Domain.Enums;
using GOP.Domain.Errors;
using GOP.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GOP.Application.Features.Wells.Commands.UpdateWell;

internal sealed class UpdateWellCommandHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdateWellCommand, Result<WellDetailDto>>
{
    public async Task<Result<WellDetailDto>> Handle(
        UpdateWellCommand request, CancellationToken cancellationToken)
    {
        // 1. Buscar el pozo
        var well = await dbContext.Wells
            .FirstOrDefaultAsync(w => w.Id == request.WellId, cancellationToken);

        if (well is null)
            return Result.Failure<WellDetailDto>(DomainErrors.Well.NotFoundById(request.WellId));

        // 2. Verificar que está en estado Borrador
        if (well.Estado != WellStatus.Borrador)
            return Result.Failure<WellDetailDto>(DomainErrors.Well.InvalidStatus);

        // 3. Validar contrato
        var contrato = await dbContext.Contratos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ContratoId, cancellationToken);

        if (contrato is null)
            return Result.Failure<WellDetailDto>(DomainErrors.Well.CampoNotBelongsToContrato);

        // 4. Validar que el campo pertenece al contrato
        var campo = await dbContext.Campos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CampoId && c.ContratoId == request.ContratoId, cancellationToken);

        if (campo is null)
            return Result.Failure<WellDetailDto>(DomainErrors.Well.CampoNotBelongsToContrato);

        // 5. Buscar departamento
        var departamento = await dbContext.Departamentos
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DepartamentoId, cancellationToken);

        if (departamento is null)
            return Result.Failure<WellDetailDto>(DomainErrors.Well.MunicipioNotBelongsToDepartamento);

        // 6. Validar que el municipio pertenece al departamento
        var municipio = await dbContext.Municipios
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.MunicipioId && m.DepartamentoId == request.DepartamentoId, cancellationToken);

        if (municipio is null)
            return Result.Failure<WellDetailDto>(DomainErrors.Well.MunicipioNotBelongsToDepartamento);

        // 7. Resolver cluster (opcional)
        string? clusterNombre = null;
        if (request.ClusterId.HasValue)
        {
            var cluster = await dbContext.Clusters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.ClusterId.Value, cancellationToken);
            clusterNombre = cluster?.Nombre;
        }

        // 8. Parsear enums
        var tipoTrayectoria = Enum.Parse<TipoTrayectoria>(request.TipoTrayectoria, ignoreCase: true);
        var clasificacion = Enum.Parse<Clasificacion>(request.Clasificacion, ignoreCase: true);
        var tipoUbicacion = Enum.Parse<TipoUbicacion>(request.TipoUbicacion, ignoreCase: true);
        var tipoAngulo = Enum.Parse<TipoAngulo>(request.TipoAngulo, ignoreCase: true);
        var tipoObjetivo = Enum.Parse<TipoObjetivo>(request.TipoObjetivo, ignoreCase: true);
        var tipoTerminacion = Enum.Parse<TipoTerminacion>(request.TipoTerminacion, ignoreCase: true);

        // 9. Construir ubicación actualizada
        var location = new WellLocation
        {
            DepartamentoId = request.DepartamentoId,
            MunicipioId = request.MunicipioId,
            ClusterId = request.ClusterId,
            CodigoDaneDpto = departamento.CodigoDane,
            CodigoDaneMpio = municipio.CodigoDane
        };

        // 10. Actualizar entidad
        well.Update(
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

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 11. Construir DTO de respuesta
        var dto = new WellDetailDto(
            Id: well.Id,
            Operadora: well.Operadora,
            ContratoId: well.ContratoId,
            Contrato: contrato.Nombre,
            TipoContrato: well.TipoContrato,
            Cuenca: well.Cuenca,
            CampoId: well.CampoId,
            Campo: campo.Nombre,
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
            Departamento: departamento.Nombre,
            CodigoDaneDpto: well.Location.CodigoDaneDpto,
            MunicipioId: well.Location.MunicipioId,
            Municipio: municipio.Nombre,
            CodigoDaneMpio: well.Location.CodigoDaneMpio,
            ClusterId: well.Location.ClusterId,
            Cluster: clusterNombre,
            CreatedAt: well.CreatedAt,
            LastModifiedAt: well.LastModifiedAt
        );

        return Result.Success(dto);
    }
}
