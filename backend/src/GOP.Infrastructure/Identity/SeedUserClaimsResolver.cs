using GOP.Application.Common.Interfaces;
using GOP.Application.Features.Auth.Queries.GetCurrentUser;
using GOP.Domain.Entities;

namespace GOP.Infrastructure.Identity;

/// <summary>
/// Implementación transitoria de IUserClaimsResolver (hasta Iter 9 — RBAC).
/// Para usuarios del seed estático de desarrollo resuelve Role/TenantId/TenantName
/// desde SeedUsers. Para usuarios no reconocidos aplica defaults de ADMIN/ANH.
/// </summary>
internal sealed class SeedUserClaimsResolver : IUserClaimsResolver
{
    public UserProfileDto BuildProfile(User user)
    {
        var seedProfile = SeedUsers.GetById(user.Id);

        if (seedProfile is not null)
            return new UserProfileDto(
                user.Id,
                user.Email,
                user.FullName,
                seedProfile.Role,
                seedProfile.TenantId,
                seedProfile.TenantName);

        // Usuarios no reconocidos en seed estático → defaults ADMIN / ANH
        return new UserProfileDto(
            user.Id,
            user.Email,
            user.FullName,
            "ADMIN",
            "1",
            "Agencia Nacional de Hidrocarburos");
    }
}