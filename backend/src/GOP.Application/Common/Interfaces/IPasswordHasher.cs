namespace GOP.Application.Common.Interfaces;

/// <summary>
/// [EMERGENTE T005b] Abstracción de verificación de contraseña.
/// Necesaria porque BCrypt.Net-Next no es dependencia de GOP.Application.
/// La implementación (BCryptPasswordHasher) vive en GOP.Infrastructure.
/// </summary>
public interface IPasswordHasher
{
    bool Verify(string password, string hash);
    string Hash(string password);
}
