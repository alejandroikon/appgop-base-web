using GOP.Domain.Common;
using GOP.Domain.Enums;

namespace GOP.Domain.Entities;

public sealed class Well : AuditableEntity
{
    // Datos del operador/tenant
    public string Operadora { get; private set; } = string.Empty;
    public int TenantId { get; private set; }

    // Relaciones de catálogo (IDs + datos desnormalizados para consulta rápida)
    public int ContratoId { get; private set; }
    public string TipoContrato { get; private set; } = string.Empty;
    public string Cuenca { get; private set; } = string.Empty;
    public int CampoId { get; private set; }

    // Clasificación técnica
    public TipoTrayectoria TipoTrayectoria { get; private set; }
    public Clasificacion Clasificacion { get; private set; }
    public TipoUbicacion TipoUbicacion { get; private set; }
    public TipoAngulo TipoAngulo { get; private set; }
    public TipoObjetivo TipoObjetivo { get; private set; }
    public TipoTerminacion TipoTerminacion { get; private set; }

    // Identificación del pozo
    public string Denominacion { get; private set; } = string.Empty;
    public string Consecutivo { get; private set; } = string.Empty;
    public string NombrePozo { get; private set; } = string.Empty;

    // Estado
    public WellStatus Estado { get; private set; } = WellStatus.Borrador;

    // Ubicación (owned entity)
    public WellLocation Location { get; private set; } = null!;

    // Constructor privado para EF Core
    private Well() { }

    public static Well Create(
        string operadora,
        int tenantId,
        int contratoId,
        string tipoContrato,
        string cuenca,
        int campoId,
        TipoTrayectoria tipoTrayectoria,
        Clasificacion clasificacion,
        string denominacion,
        string consecutivo,
        TipoUbicacion tipoUbicacion,
        TipoAngulo tipoAngulo,
        TipoObjetivo tipoObjetivo,
        TipoTerminacion tipoTerminacion,
        WellLocation location)
    {
        var nombrePozo = $"{cuenca}-{denominacion}-{consecutivo}";

        return new Well
        {
            Operadora = operadora,
            TenantId = tenantId,
            ContratoId = contratoId,
            TipoContrato = tipoContrato,
            Cuenca = cuenca,
            CampoId = campoId,
            TipoTrayectoria = tipoTrayectoria,
            Clasificacion = clasificacion,
            Denominacion = denominacion,
            Consecutivo = consecutivo,
            NombrePozo = nombrePozo,
            TipoUbicacion = tipoUbicacion,
            TipoAngulo = tipoAngulo,
            TipoObjetivo = tipoObjetivo,
            TipoTerminacion = tipoTerminacion,
            Estado = WellStatus.Borrador,
            Location = location
        };
    }

    public void Update(
        int contratoId,
        string tipoContrato,
        string cuenca,
        int campoId,
        TipoTrayectoria tipoTrayectoria,
        Clasificacion clasificacion,
        string denominacion,
        string consecutivo,
        TipoUbicacion tipoUbicacion,
        TipoAngulo tipoAngulo,
        TipoObjetivo tipoObjetivo,
        TipoTerminacion tipoTerminacion,
        WellLocation location)
    {
        ContratoId = contratoId;
        TipoContrato = tipoContrato;
        Cuenca = cuenca;
        CampoId = campoId;
        TipoTrayectoria = tipoTrayectoria;
        Clasificacion = clasificacion;
        Denominacion = denominacion;
        Consecutivo = consecutivo;
        NombrePozo = $"{cuenca}-{denominacion}-{consecutivo}";
        TipoUbicacion = tipoUbicacion;
        TipoAngulo = tipoAngulo;
        TipoObjetivo = tipoObjetivo;
        TipoTerminacion = tipoTerminacion;
        Location = location;
    }
}
