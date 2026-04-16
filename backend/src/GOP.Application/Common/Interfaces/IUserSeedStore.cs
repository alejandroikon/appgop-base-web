using GOP.Application.Features.Auth.Queries.GetCurrentUser;

namespace GOP.Application.Common.Interfaces;

public interface IUserSeedStore
{
    UserProfileDto? FindByEmail(string emailNormalized);
    UserProfileDto? GetById(Guid userId);
    bool VerifyPassword(Guid userId, string password);
}
