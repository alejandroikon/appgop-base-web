using GOP.Application.Common.Interfaces;
using GOP.Application.Features.Auth.Queries.GetCurrentUser;

namespace GOP.Infrastructure.Identity;

internal sealed class UserSeedStoreAdapter : IUserSeedStore
{
    public UserProfileDto? FindByEmail(string emailNormalized) =>
        SeedUsers.FindByEmail(emailNormalized);

    public UserProfileDto? GetById(Guid userId) =>
        SeedUsers.GetById(userId);

    public bool VerifyPassword(Guid userId, string password) =>
        SeedUsers.VerifyPassword(userId, password);
}
