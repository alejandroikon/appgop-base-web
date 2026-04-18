using GOP.Application.Common.Interfaces;
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

namespace GOP.Application.Features.Wells.Commands.CreateWell;

internal sealed class CreateWellCommandHandler(
    IApplicationDbContext dbContext,
    IWellRepository wellRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<CreateWellCommand, Result<WellDetailDto>>
{
    private const string ActionDraft = "DRAFT";
    private const string ActionFinalize = "FINALIZE";

    public async Task<Result<WellDetailDto>> Handle(
        CreateWellCommand request, CancellationToken cancellationToken)
    {
        var action = request.Action?.ToUpperInvariant();
        var isFinalize = action == ActionFinalize;

        // ─── 1. Validar campos comunes requeridos ─────────────────────────────
        if (!request.ContratoId.HasValue)
            return Result.Failure<WellDetailDto>(new Error("CreateWell.ContratoRequired", "El contrato es requerido."));

        var contrato = await dbContext.Contratos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ContratoId.Value, cancellationToken);

        if (contrato is null)
            return Result.Failure<WellDetailDto>(DomainErrors.Well.CampoNotBelongsToContrato);

        // ─── 2. Validar campo (obligatorio si Desarrollo) ─────────────────────
        Campo? campo = null;
        if (request.CampoId.HasValue)
        {
            campo = await dbContext.Campos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CampoId.Value && c.ContratoId == request.ContratoId.Value, cancellationToken);

            if (campo is null)
                return Result.Failure<WellDetailDto>(DomainErrors.Well.CampoNotBelongsToContrato);
        }

        // Parsear clasificación para validar RN-12
        Clasificacion? clasificacion = null;
        if (!string.IsNullOrWhiteSpace(request.Clasificacion) &&
            Enum.TryParse<Clasificacion>(request.Clasificacion, ignoreCase: true, out var clazParsed))
            clasificacion = clazParsed;

        if (isFinalize && clasificacion == Clasificacion.Desarrollo && campo is null)
            return Result.Failure<WellDetailDto>(DomainErrors.Well.CampoRequiredForDesarrollo);

        // ─── 3. Validar regla ANH (RN-15) ────────────────────────────────────
        var isAnhTenant = string.Equals(currentUser.TenantName, "ANH", StringComparison.OrdinalIgnoreCase);
        if (isAnhTenant && clasificacion.HasValue && clasificacion != Clasificacion.Estratigrafico)
            return Result.Failure<WellDetailDto>(DomainErrors.Well.InvalidClasificacionForAnh);

        // ─── 4. Resolver ubicación ────────────────────────────────────────────
        Departamento? departamento = null;
        Municipio? municipio = null;
        Cluster? cluster = null;

        if (request.DepartamentoId.HasValue)
        {
            departamento = await dbContext.Departamentos
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == request.DepartamentoId.Value, cancellationToken);
        }

        if (request.MunicipioId.HasValue && departamento is not null)
        {
            municipio = await dbContext.Municipios
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == request.MunicipioId.Value && m.DepartamentoId == departamento.Id, cancellationToken);

            if (municipio is null)
                return Result.Failure<WellDetailDto>(DomainErrors.Well.MunicipioNotBelongsToDepartamento);
        }

        if (request.ClusterId.HasValue)
        {
            cluster = await dbContext.Clusters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.ClusterId.Value, cancellationToken);
        }

        // ─── 5. Parsear enums restantes ───────────────────────────────────────
        SubClasificacionExploratoria? subClasificacion = null;
        if (!string.IsNullOrWhiteSpace(request.SubClasificacion) &&
            Enum.TryParse<SubClasificacionExploratoria>(request.SubClasificacion, ignoreCase: true, out var subParsed))
            subClasificacion = subParsed;

        var tipoTrayectoria = TipoTrayectoria.O;
        if (!string.IsNullOrWhiteSpace(request.TipoTrayectoria))
            Enum.TryParse(request.TipoTrayectoria, ignoreCase: true, out tipoTrayectoria);

        var tipoUbicacion = TipoUbicacion.Continental;
        if (!string.IsNullOrWhiteSpace(request.TipoUbicacion))
            Enum.TryParse(request.TipoUbicacion, ignoreCase: true, out tipoUbicacion);

        var tipoAngulo = TipoAngulo.V;
        if (!string.IsNullOrWhiteSpace(request.TipoAngulo))
            Enum.TryParse(request.TipoAngulo, ignoreCase: true, out tipoAngulo);

        var tipoObjetivo = TipoObjetivo.PH;
        if (!string.IsNullOrWhiteSpace(request.TipoObjetivo))
            Enum.TryParse(request.TipoObjetivo, ignoreCase: true, out tipoObjetivo);

        var tipoTerminacion = TipoTerminacion.OH;
        if (!string.IsNullOrWhiteSpace(request.TipoTerminacion))
            Enum.TryParse(request.TipoTerminacion, ignoreCase: true, out tipoTerminacion);

        var denominacion = request.Denominacion ?? string.Empty;
        var consecutivo = request.Consecutivo ?? 1;

        // ─── 6. Verificar nombre único por tenant ─────────────────────────────
        var campoNombrePrefijo = campo?.Nombre ?? contrato.Cuenca;
        var nombrePozo = $"{campoNombrePrefijo.ToUpperInvariant()}-{denominacion.ToUpperInvariant()}-{consecutivo}";

        var nameExists = await wellRepository.ExistsByNameAsync(
            nombrePozo, currentUser.TenantId, excludeWellId: null, cancellationToken);

        if (nameExists)
            return Result.Failure<WellDetailDto>(DomainErrors.Well.DuplicateNameValue(nombrePozo));

        // ─── 7. Flujo DRAFT vs FINALIZE ───────────────────────────────────────
        if (isFinalize)
        {
            // Requiere datos completos de ubicación
            if (departamento is null || municipio is null)
                return Result.Failure<WellDetailDto>(DomainErrors.Well.IncompleteWellData);

            // Generar UWI
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

            // Verificar unicidad global del UWI
            var uwiExists = await wellRepository.ExistsByUwiAsync(uwi.Value, null, cancellationToken);
            if (uwiExists)
                return Result.Failure<WellDetailDto>(DomainErrors.Well.DuplicateUwiValue(uwi.Value));

            var well = Well.CreateFinalized(
                operadora: currentUser.TenantName,
                tenantId: currentUser.TenantId,
                contratoId: request.ContratoId.Value,
                contrato: contrato.Nombre,
                tipoContrato: contrato.Tipo,
                cuenca: contrato.Cuenca,
                campoId: campo?.Id,
                campo: campo?.Nombre,
                denominacion: denominacion,
                consecutivo: consecutivo,
                tipoTrayectoria: tipoTrayectoria,
                clasificacion: clasificacion ?? Clasificacion.Exploratorio,
                subClasificacion: subClasificacion,
                tipoUbicacion: tipoUbicacion,
                tipoAngulo: tipoAngulo,
                tipoObjetivo: tipoObjetivo,
                tipoTerminacion: tipoTerminacion,
                departamentoId: departamento.Id,
                departamento: departamento.Nombre,
                codigoDaneDpto: departamento.CodigoDane,
                municipioId: municipio.Id,
                municipio: municipio.Nombre,
                codigoDaneMpio: ExtractMpioPart(municipio.CodigoDane),
                clusterId: cluster?.Id,
                cluster: cluster?.Nombre,
                uwi: uwi.Value);

            wellRepository.Add(well);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(ToDto(well));
        }
        else
        {
            // DRAFT: datos parciales, sin UWI
            var well = Well.CreateDraft(
                operadora: currentUser.TenantName,
                tenantId: currentUser.TenantId,
                contratoId: request.ContratoId.Value,
                contrato: contrato.Nombre,
                tipoContrato: contrato.Tipo,
                cuenca: contrato.Cuenca,
                campoId: campo?.Id,
                campo: campo?.Nombre,
                denominacion: denominacion,
                consecutivo: consecutivo,
                tipoTrayectoria: tipoTrayectoria,
                clasificacion: clasificacion ?? Clasificacion.Exploratorio,
                subClasificacion: subClasificacion,
                tipoUbicacion: tipoUbicacion,
                tipoAngulo: tipoAngulo,
                tipoObjetivo: tipoObjetivo,
                tipoTerminacion: tipoTerminacion,
                departamentoId: departamento?.Id,
                departamento: departamento?.Nombre,
                codigoDaneDpto: departamento?.CodigoDane,
                municipioId: municipio?.Id,
                municipio: municipio?.Nombre,
                codigoDaneMpio: municipio is not null ? ExtractMpioPart(municipio.CodigoDane) : null,
                clusterId: cluster?.Id,
                cluster: cluster?.Nombre);

            wellRepository.Add(well);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(ToDto(well));
        }
    }

    /// <summary>Extrae los 3 últimos dígitos del código DANE completo (5 dígitos) del municipio.</summary>
    private static string ExtractMpioPart(string codigoDane5)
    {
        if (codigoDane5.Length >= 5)
            return codigoDane5[2..]; // últimos 3 dígitos
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
