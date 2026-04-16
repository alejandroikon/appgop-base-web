using System.Security.Claims;
using GOP.Application.Features.Auth.Queries.GetCurrentUser;

namespace GOP.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(UserProfileDto user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
