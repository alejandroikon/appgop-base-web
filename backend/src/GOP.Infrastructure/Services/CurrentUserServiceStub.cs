using GOP.Domain.Interfaces.Services;

namespace GOP.Infrastructure.Services;

/// <summary>
/// Stub de ICurrentUserService — retorna valores dummy hasta que se implemente JWT.
/// Se reemplaza por la implementación real en la feature de autenticación.
/// </summary>
internal sealed class CurrentUserServiceStub : ICurrentUserService
{
    public Guid UserId => Guid.Empty;
    public string Email => "system@gop360.local";
    public string Name => "System";
    public string Role => "SYSTEM";
    public int TenantId => 0;
    public bool IsAuthenticated => false;

    public bool IsInRole(string role) => false;
}
