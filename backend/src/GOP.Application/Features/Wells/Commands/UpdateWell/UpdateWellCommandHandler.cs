using GOP.Application.Common.Interfaces;
using GOP.Application.Features.Wells.Commands.CreateWell;
using GOP.Application.Features.Wells.Queries.GetWellById;
using GOP.Domain.Common;
using GOP.Domain.Entities;
using GOP.Domain.Enums;
using GOP.Domain.Errors;
using GOP.Domain.Interfaces;
using GOP.Domain.Interfaces.Repositories;
using GOP.Domain.Interfaces.Services;
using GOP.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GOP.Application.Features.Wells.Commands.UpdateWell;

internal sealed class UpdateWellCommandHandler(
    IApplicationDbContext dbContext,
    IWellRepository wellRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<UpdateWellCommand, Result<WellDetailDto>>
{
    private const string ActionFinalize = "FINALIZE";

    public async Task<Result<WellDetailDto>> Handle(
        UpdateWellCommand request, CancellationToken cancellationToken)
    {
        // ─── 1. Buscar el pozo ────────────────────────────────────────────────
        var well = await dbContext.Wells
            .FirstOrDefaultAsync(w => w.Id == request.WellId, cancellationToken);

        if (well is null)
            return Result.Failure<WellDetailDto>(DomainErrors.Well.NotFoundById(request.WellId));

        // ─── 2. Guard RN-40 ───────────────────────────────────────────────────
        if (!well.IsEditable())
            return Result.Failure<WellDetailDto>(DomainErrors.Well.NotEditable);

        // ─── 3. Resolver catálogos ────────────────────────────────────────────
        var contratoId = request.ContratoId ?? well.ContratoId;
        var contrato = await dbContext.Contratos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == contratoId, cancellationToken);

        if (contrato is null)
            return Result.Failure<WellDetailDto>(DomainErrors.Well.CampoNotBelongsToContrato);

        Campo? campo = null;
        if (request.CampoId.HasValue)
        {
            campo = await dbContext.Campos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CampoId.Value && c.ContratoId == contratoId, cancellationToken);

            if (campo is null)
                return Result.Failure<WellDetailDto>(DomainErrors.Well.CampoNotBelongsToContrato);
        }

        // Parsear enums
        Clasificacion? clasificacion = null;
        if (!string.IsNullOrWhiteSpace(request.Clasificacion) &&
            Enum.TryParse<Clasificacion>(request.Clasificacion, ignoreCase: true, out var clazParsed))
            clasificacion = clazParsed;

        var isFinalize = request.Action?.ToUpperInvariant() == ActionFinalize;
        if (isFinalize && clasificacion == Clasificacion.Desarrollo && campo is null)
            return Result.Failure<WellDetailDto>(DomainErrors.Well.CampoRequiredForDesarrollo);

        SubClasificacionExploratoria? subClasificacion = null;
        if (!string.IsNullOrWhiteSpace(request.SubClasificacion) &&
            Enum.TryParse<SubClasificacionExploratoria>(request.SubClasificacion, ignoreCase: true, out var subParsed))
            subClasificacion = subParsed;

        var tipoTrayectoria = well.TipoTrayectoria;
        if (!string.IsNullOrWhiteSpace(request.TipoTrayectoria))
            Enum.TryParse(request.TipoTrayectoria, ignoreCase: true, out tipoTrayectoria);

        var tipoUbicacion = well.TipoUbicacion;
        if (!string.IsNullOrWhiteSpace(request.TipoUbicacion))
            Enum.TryParse(request.TipoUbicacion, ignoreCase: true, out tipoUbicacion);

        var tipoAngulo = well.TipoAngulo;
        if (!string.IsNullOrWhiteSpace(request.TipoAngulo))
            Enum.TryParse(request.TipoAngulo, ignoreCase: true, out tipoAngulo);

        var tipoObjetivo = well.TipoObjetivo;
        if (!string.IsNullOrWhiteSpace(request.TipoObjetivo))
            Enum.TryParse(request.TipoObjetivo, ignoreCase: true, out tipoObjetivo);

        var tipoTerminacion = well.TipoTerminacion;
        if (!string.IsNullOrWhiteSpace(request.TipoTerminacion))
            Enum.TryParse(request.TipoTerminacion, ignoreCase: true, out tipoTerminacion);

        // ─── 4. Resolver ubicación ────────────────────────────────────────────
        Departamento? departamento = null;
        Municipio? municipio = null;
        Cluster? cluster = null;

        var deptId = request.DepartamentoId ?? well.DepartamentoId;
        if (deptId.HasValue)
        {
            departamento = await dbContext.Departamentos
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == deptId.Value, cancellationToken);
        }

        var mpioId = request.MunicipioId ?? well.MunicipioId;
        if (mpioId.HasValue && departamento is not null)
        {
            municipio = await dbContext.Municipios
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == mpioId.Value && m.DepartamentoId == departamento.Id, cancellationToken);
        }

        var clusterId = request.ClusterId ?? well.ClusterId;
        if (clusterId.HasValue)
        {
            cluster = await dbContext.Clusters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == clusterId.Value, cancellationToken);
        }

        var denominacion = request.Denominacion ?? well.Denominacion;
        var consecutivo = request.Consecutivo ?? well.Consecutivo;
        var clazFinal = clasificacion ?? well.Clasificacion;

        // ─── 5. Verificar nombre único (si cambió) ────────────────────────────
        var campoPrefix = campo?.Nombre ?? contrato.Cuenca;
        var nombrePozo = $"{campoPrefix.ToUpperInvariant()}-{denominacion.ToUpperInvariant()}-{consecutivo}";
        if (nombrePozo != well.NombrePozo)
        {
            var nameExists = await wellRepository.ExistsByNameAsync(
                nombrePozo, currentUser.TenantId, well.Id, cancellationToken);
            if (nameExists)
                return Result.Failure<WellDetailDto>(DomainErrors.Well.DuplicateNameValue(nombrePozo));
        }

        // ─── 6. Actualizar propiedades ────────────────────────────────────────
        var updateResult = well.Update(
            contratoId: contratoId,
            contrato: contrato.Nombre,
            tipoContrato: contrato.Tipo,
            cuenca: contrato.Cuenca,
            campoId: campo?.Id,
            campo: campo?.Nombre,
            denominacion: denominacion,
            consecutivo: consecutivo,
            tipoTrayectoria: tipoTrayectoria,
            clasificacion: clazFinal,
            subClasificacion: subClasificacion,
            tipoUbicacion: tipoUbicacion,
            tipoAngulo: tipoAngulo,
            tipoObjetivo: tipoObjetivo,
            tipoTerminacion: tipoTerminacion,
            departamentoId: departamento?.Id ?? well.DepartamentoId,
            departamento: departamento?.Nombre ?? well.Departamento,
            codigoDaneDpto: departamento?.CodigoDane ?? well.CodigoDaneDpto,
            municipioId: municipio?.Id ?? well.MunicipioId,
            municipio: municipio?.Nombre ?? well.Municipio,
            codigoDaneMpio: municipio is not null
                ? ExtractMpioPart(municipio.CodigoDane)
                : well.CodigoDaneMpio,
            clusterId: cluster?.Id ?? (request.ClusterId.HasValue ? null : well.ClusterId),
            cluster: cluster?.Nombre ?? (request.ClusterId.HasValue ? null : well.Cluster));

        if (updateResult.IsFailure)
            return Result.Failure<WellDetailDto>(updateResult.Error);

        // ─── 7. FINALIZE: generar UWI si es borrador ──────────────────────────
        if (isFinalize && well.Estado == WellStatus.Borrador)
        {
            if (departamento is null || municipio is null)
                return Result.Failure<WellDetailDto>(DomainErrors.Well.IncompleteWellData);

            var isAnhTenant = string.Equals(currentUser.TenantName, "ANH", StringComparison.OrdinalIgnoreCase);
            var uwiResult = Uwi.Generate(
                codigoDaneDpto: departamento.CodigoDane,
                codigoDaneMpio: ExtractMpioPart(municipio.CodigoDane),
                denominacion: denominacion,
                consecutivo: consecutivo,
                clusterNombre: cluster?.Abreviatura ?? cluster?.Nombre,
                clusterNumero: request.ClusterNumero,
                tipoAngulo: tipoAngulo,
                tipoTrayectoria: tipoTrayectoria,
                trayectoriaConsecutivo: request.TrayectoriaConsecutivo,
                tipoObjetivo: tipoObjetivo,
                tipoTerminacion: tipoTerminacion,
                isAnh: isAnhTenant);

            if (uwiResult.IsFailure)
                return Result.Failure<WellDetailDto>(uwiResult.Error);

            var uwi = uwiResult.Value;
            var uwiExists = await wellRepository.ExistsByUwiAsync(uwi.Value, well.Id, cancellationToken);
            if (uwiExists)
                return Result.Failure<WellDetailDto>(DomainErrors.Well.DuplicateUwiValue(uwi.Value));

            var finalizeResult = well.Finalize(uwi.Value);
            if (finalizeResult.IsFailure)
                return Result.Failure<WellDetailDto>(finalizeResult.Error);
        }

        wellRepository.Update(well);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(ToDto(well));
    }

    private static string ExtractMpioPart(string codigoDane5)
    {
        if (codigoDane5.Length >= 5)
            return codigoDane5[2..];
        return codigoDane5.PadLeft(3, '0');
    }

    private static WellDetailDto ToDto(Well w) => new(
        Id: w.Id,
        Operadora: w.Operadora,
        ContratoId: w.ContratoId,
        Contrato: w.Contrato,
        TipoContrato: w.TipoContrato,
        Cuenca: w.Cuenca,
        CampoId: w.CampoId,
        Campo: w.Campo,
        Denominacion: w.Denominacion,
        Consecutivo: w.Consecutivo,
        NombrePozo: w.NombrePozo,
        TipoTrayectoria: w.TipoTrayectoria.ToString(),
        Clasificacion: w.Clasificacion.ToString().ToUpperInvariant(),
        SubClasificacion: w.SubClasificacion?.ToString(),
        TipoUbicacion: w.TipoUbicacion.ToString().ToUpperInvariant(),
        TipoAngulo: w.TipoAngulo.ToString(),
        TipoObjetivo: w.TipoObjetivo.ToString(),
        TipoTerminacion: w.TipoTerminacion.ToString(),
        Estado: w.Estado.ToString().ToUpperInvariant(),
        Uwi: w.Uwi,
        Forma101Radicada: w.Forma101Radicada,
        DepartamentoId: w.DepartamentoId,
        Departamento: w.Departamento,
        CodigoDaneDpto: w.CodigoDaneDpto,
        MunicipioId: w.MunicipioId,
        Municipio: w.Municipio,
        CodigoDaneMpio: w.CodigoDaneMpio,
        ClusterId: w.ClusterId,
        Cluster: w.Cluster,
        CreatedAt: w.CreatedAt,
        LastModifiedAt: w.LastModifiedAt
    );
}
