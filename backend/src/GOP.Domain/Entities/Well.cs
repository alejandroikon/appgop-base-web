using GOP.Domain.Common;
using GOP.Domain.Enums;
using GOP.Domain.Errors;

namespace GOP.Domain.Entities;

public sealed class Well : AuditableEntity
{
    // ─── Identificación ──────────────────────────────────────────────────────
    public string NombrePozo { get; private set; } = string.Empty;
    public string? Uwi { get; private set; }
    public string Operadora { get; private set; } = string.Empty;

    // ─── Contrato ────────────────────────────────────────────────────────────
    public int ContratoId { get; private set; }
    public string Contrato { get; private set; } = string.Empty;
    public string TipoContrato { get; private set; } = string.Empty;
    public string Cuenca { get; private set; } = string.Empty;

    // ─── Campo ───────────────────────────────────────────────────────────────
    public int? CampoId { get; private set; }
    public string? Campo { get; private set; }

    // ─── Datos Técnicos ──────────────────────────────────────────────────────
    public string Denominacion { get; private set; } = string.Empty;
    public int Consecutivo { get; private set; }
    public TipoTrayectoria TipoTrayectoria { get; private set; }
    public Clasificacion Clasificacion { get; private set; }
    public SubClasificacionExploratoria? SubClasificacion { get; private set; }
    public TipoUbicacion TipoUbicacion { get; private set; }
    public TipoAngulo TipoAngulo { get; private set; }
    public TipoObjetivo TipoObjetivo { get; private set; }
    public TipoTerminacion TipoTerminacion { get; private set; }

    // ─── Ubicación Geográfica (aplanada) ─────────────────────────────────────
    public int? DepartamentoId { get; private set; }
    public string? Departamento { get; private set; }
    public string? CodigoDaneDpto { get; private set; }
    public int? MunicipioId { get; private set; }
    public string? Municipio { get; private set; }
    public string? CodigoDaneMpio { get; private set; }
    public int? ClusterId { get; private set; }
    public string? Cluster { get; private set; }

    // ─── Estado ──────────────────────────────────────────────────────────────
    public WellStatus Estado { get; private set; } = WellStatus.Borrador;
    public bool Forma101Radicada { get; private set; }

    // ─── Multi-Tenancy ───────────────────────────────────────────────────────
    public int TenantId { get; private set; }

    // Constructor privado para EF Core
    private Well() { }

    /// <summary>Crea un borrador con datos mínimos (sin UWI). RN-32.</summary>
    public static Well CreateDraft(
        string operadora,
        int tenantId,
        int contratoId,
        string contrato,
        string tipoContrato,
        string cuenca,
        int? campoId,
        string? campo,
        string denominacion,
        int consecutivo,
        TipoTrayectoria tipoTrayectoria,
        Clasificacion clasificacion,
        SubClasificacionExploratoria? subClasificacion,
        TipoUbicacion tipoUbicacion,
        TipoAngulo tipoAngulo,
        TipoObjetivo tipoObjetivo,
        TipoTerminacion tipoTerminacion,
        int? departamentoId,
        string? departamento,
        string? codigoDaneDpto,
        int? municipioId,
        string? municipio,
        string? codigoDaneMpio,
        int? clusterId,
        string? cluster)
    {
        var nombrePozo = BuildNombrePozo(campo ?? cuenca, denominacion, consecutivo);

        return new Well
        {
            Operadora = operadora,
            TenantId = tenantId,
            ContratoId = contratoId,
            Contrato = contrato,
            TipoContrato = tipoContrato,
            Cuenca = cuenca,
            CampoId = campoId,
            Campo = campo,
            Denominacion = denominacion.ToUpperInvariant(),
            Consecutivo = consecutivo,
            NombrePozo = nombrePozo,
            TipoTrayectoria = tipoTrayectoria,
            Clasificacion = clasificacion,
            SubClasificacion = subClasificacion,
            TipoUbicacion = tipoUbicacion,
            TipoAngulo = tipoAngulo,
            TipoObjetivo = tipoObjetivo,
            TipoTerminacion = tipoTerminacion,
            Estado = WellStatus.Borrador,
            DepartamentoId = departamentoId,
            Departamento = departamento,
            CodigoDaneDpto = codigoDaneDpto,
            MunicipioId = municipioId,
            Municipio = municipio,
            CodigoDaneMpio = codigoDaneMpio,
            ClusterId = clusterId,
            Cluster = cluster
        };
    }

    /// <summary>Crea un pozo finalizado con todos los datos y UWI generado. RN-33.</summary>
    public static Well CreateFinalized(
        string operadora,
        int tenantId,
        int contratoId,
        string contrato,
        string tipoContrato,
        string cuenca,
        int? campoId,
        string? campo,
        string denominacion,
        int consecutivo,
        TipoTrayectoria tipoTrayectoria,
        Clasificacion clasificacion,
        SubClasificacionExploratoria? subClasificacion,
        TipoUbicacion tipoUbicacion,
        TipoAngulo tipoAngulo,
        TipoObjetivo tipoObjetivo,
        TipoTerminacion tipoTerminacion,
        int departamentoId,
        string departamento,
        string codigoDaneDpto,
        int municipioId,
        string municipio,
        string codigoDaneMpio,
        int? clusterId,
        string? cluster,
        string uwi)
    {
        var nombrePozo = BuildNombrePozo(campo ?? cuenca, denominacion, consecutivo);

        return new Well
        {
            Operadora = operadora,
            TenantId = tenantId,
            ContratoId = contratoId,
            Contrato = contrato,
            TipoContrato = tipoContrato,
            Cuenca = cuenca,
            CampoId = campoId,
            Campo = campo,
            Denominacion = denominacion.ToUpperInvariant(),
            Consecutivo = consecutivo,
            NombrePozo = nombrePozo,
            TipoTrayectoria = tipoTrayectoria,
            Clasificacion = clasificacion,
            SubClasificacion = subClasificacion,
            TipoUbicacion = tipoUbicacion,
            TipoAngulo = tipoAngulo,
            TipoObjetivo = tipoObjetivo,
            TipoTerminacion = tipoTerminacion,
            Estado = WellStatus.Creado,
            DepartamentoId = departamentoId,
            Departamento = departamento,
            CodigoDaneDpto = codigoDaneDpto,
            MunicipioId = municipioId,
            Municipio = municipio,
            CodigoDaneMpio = codigoDaneMpio,
            ClusterId = clusterId,
            Cluster = cluster,
            Uwi = uwi
        };
    }

    // ─── Reglas de negocio ────────────────────────────────────────────────────

    /// <summary>
    /// Marca el pozo como con Forma 101 radicada (bloqueo RN-40).
    /// Solo debe llamarse desde la capa de Application al registrar la Forma 101.
    /// </summary>
    public void MarkForma101Radicada() => Forma101Radicada = true;

    /// <summary>
    /// RN-40: Un pozo es editable si está en Borrador, o si está Creado sin Forma 101 radicada.
    /// </summary>
    public bool IsEditable() =>
        Estado == WellStatus.Borrador || (Estado == WellStatus.Creado && !Forma101Radicada);

    /// <summary>
    /// RN-40: Un pozo es eliminable bajo las mismas condiciones que IsEditable.
    /// </summary>
    public bool IsDeletable() => IsEditable();

    /// <summary>Actualiza los datos del pozo (solo si IsEditable).</summary>
    public Result Update(
        int contratoId,
        string contrato,
        string tipoContrato,
        string cuenca,
        int? campoId,
        string? campo,
        string denominacion,
        int consecutivo,
        TipoTrayectoria tipoTrayectoria,
        Clasificacion clasificacion,
        SubClasificacionExploratoria? subClasificacion,
        TipoUbicacion tipoUbicacion,
        TipoAngulo tipoAngulo,
        TipoObjetivo tipoObjetivo,
        TipoTerminacion tipoTerminacion,
        int? departamentoId,
        string? departamento,
        string? codigoDaneDpto,
        int? municipioId,
        string? municipio,
        string? codigoDaneMpio,
        int? clusterId,
        string? cluster)
    {
        if (!IsEditable())
            return Result.Failure(DomainErrors.Well.NotEditable);

        ContratoId = contratoId;
        Contrato = contrato;
        TipoContrato = tipoContrato;
        Cuenca = cuenca;
        CampoId = campoId;
        Campo = campo;
        Denominacion = denominacion.ToUpperInvariant();
        Consecutivo = consecutivo;
        NombrePozo = BuildNombrePozo(campo ?? cuenca, denominacion, consecutivo);
        TipoTrayectoria = tipoTrayectoria;
        Clasificacion = clasificacion;
        SubClasificacion = subClasificacion;
        TipoUbicacion = tipoUbicacion;
        TipoAngulo = tipoAngulo;
        TipoObjetivo = tipoObjetivo;
        TipoTerminacion = tipoTerminacion;
        DepartamentoId = departamentoId;
        Departamento = departamento;
        CodigoDaneDpto = codigoDaneDpto;
        MunicipioId = municipioId;
        Municipio = municipio;
        CodigoDaneMpio = codigoDaneMpio;
        ClusterId = clusterId;
        Cluster = cluster;

        return Result.Success();
    }

    /// <summary>
    /// Finaliza el borrador: asigna UWI y cambia estado a Creado. RN-33.
    /// Solo válido desde Borrador.
    /// </summary>
    public Result Finalize(string uwi)
    {
        if (Estado != WellStatus.Borrador)
            return Result.Failure(new Error("Well.AlreadyFinalized", "El pozo ya fue finalizado."));

        Uwi = uwi;
        Estado = WellStatus.Creado;
        return Result.Success();
    }

    /// <summary>Soft delete: marca el pozo como eliminado si es deletable. RN-40.</summary>
    public Result SoftDelete()
    {
        if (!IsDeletable())
            return Result.Failure(DomainErrors.Well.NotDeletable);

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        return Result.Success();
    }

    // ─── Factory privado ──────────────────────────────────────────────────────

    /// <summary>
    /// Genera el nombre compuesto del pozo.
    /// Formato: {Campo}-{DENOMINACION}-{Consecutivo}
    /// </summary>
    private static string BuildNombrePozo(string campoOCuenca, string denominacion, int consecutivo)
        => $"{campoOCuenca.ToUpperInvariant()}-{denominacion.ToUpperInvariant()}-{consecutivo}";
}
