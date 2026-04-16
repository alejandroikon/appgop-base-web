using System.Security.Claims;
using GOP.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace GOP.Infrastructure.Identity;

internal sealed class CurrentUserService(
    IHttpContextAccessor httpContextAccessor
) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            var sub = User?.FindFirst("sub")?.Value
                   ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
        }
    }

    public string Email =>
        User?.FindFirst("email")?.Value
        ?? User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
        ?? string.Empty;

    public string Name =>
        User?.FindFirst("name")?.Value
        ?? User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
        ?? string.Empty;

    public string Role =>
        User?.FindFirst("role")?.Value
        ?? User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
        ?? string.Empty;

    public int TenantId
    {
        get
        {
            var tenantIdValue = User?.FindFirst("tenant_id")?.Value;
            return int.TryParse(tenantIdValue, out var id) ? id : 0;
        }
    }

    public string TenantName =>
        User?.FindFirst("tenant_name")?.Value ?? string.Empty;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) =>
        User?.IsInRole(role) ?? false;
}
