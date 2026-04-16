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
}
