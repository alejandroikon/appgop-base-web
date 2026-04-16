using GOP.Application.Features.Auth.Queries.GetCurrentUser;

namespace GOP.Application.Features.Auth.Dtos;

public sealed record TokenResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserProfileDto User);
