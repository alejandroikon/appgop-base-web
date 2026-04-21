using GOP.Application.Common.Interfaces;

namespace GOP.Infrastructure.Identity;

/// <summary>
/// [EMERGENTE T015b] Implementación concreta de IPasswordHasher usando BCrypt.Net-Next.
/// BCrypt.Net-Next es dependencia de GOP.Infrastructure, no de GOP.Application —
/// esta clase actúa como adaptador entre la abstracción limpia y la biblioteca concreta.
/// </summary>
internal sealed class BCryptPasswordHasher : IPasswordHasher
{
    public bool Verify(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);

    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password);
}