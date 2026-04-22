using GOP.Application.Features.Auth.Queries.GetCurrentUser;
using GOP.Domain.Entities;

namespace GOP.Application.Common.Interfaces;

/// <summary>
/// Transitorio hasta Iter 9 (RBAC). Encapsula la resolución de Role/TenantId/TenantName
/// para un User que aún no tiene esos campos en la entidad.
/// </summary>
public interface IUserClaimsResolver
{
    UserProfileDto BuildProfile(User user);
}
