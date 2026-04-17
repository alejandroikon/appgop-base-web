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

        public static readonly Error InvalidStatus = new(
            "Well.InvalidStatus",
            "Solo se pueden editar pozos en estado borrador.");

        public static readonly Error DeleteInvalidStatus = new(
            "Well.DeleteInvalidStatus",
            "Solo se pueden eliminar pozos en estado borrador.");

        public static readonly Error CampoNotBelongsToContrato = new(
            "Well.CampoNotBelongsToContrato",
            "El campo seleccionado no pertenece al contrato indicado.");

        public static readonly Error MunicipioNotBelongsToDepartamento = new(
            "Well.MunicipioNotBelongsToDepartamento",
            "El municipio seleccionado no pertenece al departamento indicado.");

        public static Error InvalidTransition(string action, string currentState) => new(
            "Well.InvalidTransition",
            $"La acción {action} no es válida desde el estado {currentState}.");

        public static readonly Error TransitionUnauthorized = new(
            "Well.TransitionUnauthorized",
            "No tiene permisos para ejecutar esta transición.");

        public static readonly Error DuplicateUwi = new(
            "Well.DuplicateUwi",
            "Ya existe un pozo con el UWI generado.");

        public static readonly Error CommentRequired = new(
            "Well.CommentRequired",
            "El motivo de devolución es requerido.");

        public static readonly Error IncompleteWellData = new(
            "Well.IncompleteWellData",
            "El pozo tiene campos requeridos sin completar.");

        public static Error InvalidEnumValue(string fieldName, string value) => new(
            "Well.InvalidFieldValue",
            $"El valor '{value}' no es válido para el campo '{fieldName}'.");

        public static readonly Error FiscalizedImmutable = new(
            "Well.FiscalizedImmutable",
            "No se puede modificar un pozo fiscalizado.");
    }
}
