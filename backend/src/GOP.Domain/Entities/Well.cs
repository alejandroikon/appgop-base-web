using GOP.Domain.Common;
using GOP.Domain.Enums;
using GOP.Domain.Errors;

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

    // UWI — generado en transición ENVIAR
    public string? Uwi { get; private set; }

    // Ubicación (owned entity)
    public WellLocation Location { get; private set; } = null!;

    // ─── Máquina de estados ──────────────────────────────────────────────
    private static readonly Dictionary<(WellStatus From, TransitionAction Action), WellStatus> ValidTransitions = new()
    {
        { (WellStatus.Borrador, TransitionAction.Enviar), WellStatus.PendingUwi },
        { (WellStatus.PendingUwi, TransitionAction.AprobarUwi), WellStatus.ReadyFiscal },
        { (WellStatus.PendingUwi, TransitionAction.Devolver), WellStatus.Borrador },
        { (WellStatus.ReadyFiscal, TransitionAction.Fiscalizar), WellStatus.Fiscalizado },
        { (WellStatus.ReadyFiscal, TransitionAction.Devolver), WellStatus.Borrador },
    };

    private static readonly Dictionary<TransitionAction, string[]> AuthorizedRoles = new()
    {
        { TransitionAction.Enviar,     new[] { "ADMIN", "SUPERVISOR", "OPERADOR" } },
        { TransitionAction.AprobarUwi, new[] { "ADMIN", "SUPERVISOR" } },
        { TransitionAction.Devolver,   new[] { "ADMIN", "SUPERVISOR" } },
        { TransitionAction.Fiscalizar, new[] { "ADMIN", "SUPERVISOR" } },
    };

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

    // ─── Métodos de transición ───────────────────────────────────────────

    public void SetUwi(string uwi) => Uwi = uwi;

    /// <summary>Valida si la transición es válida para el estado actual y el rol del usuario.</summary>
    public Result CanTransition(TransitionAction action, string userRole)
    {
        if (Estado == WellStatus.Fiscalizado)
            return Result.Failure(DomainErrors.Well.FiscalizedImmutable);

        if (!AuthorizedRoles.TryGetValue(action, out var roles) || !roles.Contains(userRole))
            return Result.Failure(DomainErrors.Well.TransitionUnauthorized);

        if (!ValidTransitions.ContainsKey((Estado, action)))
            return Result.Failure(DomainErrors.Well.InvalidTransition(action.ToString(), Estado.ToString()));

        return Result.Success();
    }

    /// <summary>Ejecuta la transición: valida y cambia el estado.</summary>
    public Result ApplyTransition(TransitionAction action, string userRole, string? comment)
    {
        if (action == TransitionAction.Devolver && string.IsNullOrWhiteSpace(comment))
            return Result.Failure(DomainErrors.Well.CommentRequired);

        var canResult = CanTransition(action, userRole);
        if (canResult.IsFailure) return canResult;

        Estado = ValidTransitions[(Estado, action)];
        return Result.Success();
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
