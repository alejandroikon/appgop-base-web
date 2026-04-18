using GOP.Domain.Common;

namespace GOP.Domain.Errors;

public static partial class DomainErrors
{
    public static class Auth
    {
        public static readonly Error InvalidCredentials = new(
            "Auth.InvalidCredentials",
            "Correo o contraseña incorrectos.");

        public static readonly Error TokenExpired = new(
            "Auth.TokenExpired",
            "El token de actualización ha expirado. Inicie sesión nuevamente.");

        public static readonly Error InvalidRefreshToken = new(
            "Auth.InvalidRefreshToken",
            "Token de actualización inválido.");
    }

    public static class Well
    {
        public static readonly Error NotFound = new(
            "Well.NotFound",
            "El pozo no fue encontrado.");

        public static Error NotFoundById(Guid id) => new(
            "Well.NotFound",
            $"No se encontró un pozo con Id '{id}'.");

        // ─── Errores de estado/edición ───────────────────────────────────────
        public static readonly Error InvalidStatus = new(
            "Well.InvalidStatus",
            "Solo se pueden editar pozos en estado borrador o creado sin Forma 101 radicada.");

        public static readonly Error DeleteInvalidStatus = new(
            "Well.DeleteInvalidStatus",
            "Solo se pueden eliminar pozos en estado borrador o creado sin Forma 101 radicada.");

        /// <summary>RN-40: Pozo bloqueado porque tiene Forma 101 radicada.</summary>
        public static readonly Error NotEditable = new(
            "Well.NotEditable",
            "Este pozo no puede editarse porque tiene Forma 101 radicada.");

        /// <summary>RN-40: Pozo no puede eliminarse porque tiene Forma 101 radicada.</summary>
        public static readonly Error NotDeletable = new(
            "Well.NotDeletable",
            "Este pozo no puede eliminarse porque tiene Forma 101 radicada.");

        /// <summary>RN-40: Alias semántico de NotEditable para usar en contexto de bloqueo.</summary>
        public static readonly Error Forma101Locked = new(
            "Well.Forma101Locked",
            "Este pozo está bloqueado porque tiene Forma 101 radicada.");

        // ─── Errores de unicidad ─────────────────────────────────────────────
        /// <summary>RN-37: UWI duplicado a nivel global.</summary>
        public static readonly Error DuplicateUwi = new(
            "Well.DuplicateUwi",
            "Ya existe un pozo con el UWI generado.");

        public static Error DuplicateUwiValue(string uwi) => new(
            "Well.DuplicateUwi",
            $"Ya existe un pozo con el UWI '{uwi}'.");

        /// <summary>RN-01: Nombre de pozo duplicado dentro de la misma operadora.</summary>
        public static readonly Error DuplicateName = new(
            "Well.DuplicateName",
            "Ya existe un pozo con ese nombre en la misma operadora.");

        public static Error DuplicateNameValue(string name) => new(
            "Well.DuplicateName",
            $"Ya existe un pozo con el nombre '{name}' en la misma operadora.");

        // ─── Errores de reglas de negocio ────────────────────────────────────
        /// <summary>RN-15: Prefijo ANH solo permitido para pozos estratigráficos.</summary>
        public static readonly Error InvalidClasificacionForAnh = new(
            "Well.InvalidClasificacionForAnh",
            "El prefijo ANH solo está permitido para pozos estratigráficos.");

        /// <summary>RN-12: Campo obligatorio cuando clasificación es Desarrollo.</summary>
        public static readonly Error CampoRequiredForDesarrollo = new(
            "Well.CampoRequiredForDesarrollo",
            "El campo es obligatorio para pozos de clasificación Desarrollo.");

        public static readonly Error CampoNotBelongsToContrato = new(
            "Well.CampoNotBelongsToContrato",
            "El campo seleccionado no pertenece al contrato indicado.");

        public static readonly Error MunicipioNotBelongsToDepartamento = new(
            "Well.MunicipioNotBelongsToDepartamento",
            "El municipio seleccionado no pertenece al departamento indicado.");

        public static readonly Error CommentRequired = new(
            "Well.CommentRequired",
            "El motivo de devolución es requerido.");

        public static readonly Error IncompleteWellData = new(
            "Well.IncompleteWellData",
            "El pozo tiene campos requeridos sin completar.");

        public static Error InvalidEnumValue(string fieldName, string value) => new(
            "Well.InvalidFieldValue",
            $"El valor '{value}' no es válido para el campo '{fieldName}'.");

        // ─── Legacy (mantener para compatibilidad) ───────────────────────────
        public static readonly Error FiscalizedImmutable = new(
            "Well.FiscalizedImmutable",
            "No se puede modificar un pozo fiscalizado.");
    }

    public static class Cluster
    {
        public static readonly Error DuplicateInCampo = new(
            "Cluster.DuplicateInCampo",
            "Ya existe un cluster con ese nombre en el campo seleccionado.");

        public static readonly Error NotFound = new(
            "Cluster.NotFound",
            "El cluster no fue encontrado.");
    }
}
