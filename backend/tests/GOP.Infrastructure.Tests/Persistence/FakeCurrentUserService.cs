using GOP.Domain.Interfaces.Services;

namespace GOP.Infrastructure.Tests.Persistence;

/// <summary>
/// Implementación fake de ICurrentUserService para tests de infraestructura
/// que no prueban lógica multi-tenant directamente.
/// TenantId es configurable para soportar tests de aislamiento (RN-23/RN-26).
/// </summary>
internal sealed class FakeCurrentUserService : ICurrentUserService
{
    public static readonly Guid DefaultUserId =
        new("00000000-0000-0000-0000-000000000002");

    public FakeCurrentUserService(int tenantId = 1)
    {
        TenantId = tenantId;
    }

    public Guid UserId { get; } = DefaultUserId;
    public string Email { get; } = "fake@gop.co";
    public string Name { get; } = "Fake User";
    public string Role { get; } = "ADMIN";
    public int TenantId { get; }
    public string TenantName { get; } = "Fake Tenant";
    public bool IsAuthenticated { get; } = true;

    public bool IsInRole(string role) => Role == role;
}
