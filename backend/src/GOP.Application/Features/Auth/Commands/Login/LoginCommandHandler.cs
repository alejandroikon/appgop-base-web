using GOP.Application.Common.Interfaces;
using GOP.Application.Features.Auth.Dtos;
using GOP.Domain.Common;
using GOP.Domain.Errors;
using MediatR;
using Microsoft.Extensions.Options;

namespace GOP.Application.Features.Auth.Commands.Login;

internal sealed class LoginCommandHandler(
    IUserSeedStore userStore,
    IJwtTokenService jwtTokenService,
    IRefreshTokenStore refreshTokenStore,
    IOptions<JwtTokenOptions> jwtOptions
) : IRequestHandler<LoginCommand, Result<TokenResponseDto>>
{
    public Task<Result<TokenResponseDto>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = userStore.FindByEmail(normalizedEmail);
        if (user is null)
            return Task.FromResult(Result.Failure<TokenResponseDto>(DomainErrors.Auth.InvalidCredentials));

        if (!userStore.VerifyPassword(user.Id, request.Password))
            return Task.FromResult(Result.Failure<TokenResponseDto>(DomainErrors.Auth.InvalidCredentials));

        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var refreshToken = jwtTokenService.GenerateRefreshToken();

        var expiresAt = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationDays);
        refreshTokenStore.Store(refreshToken, user.Id, expiresAt);

        var response = new TokenResponseDto(
            accessToken,
            refreshToken,
            jwtOptions.Value.AccessTokenExpirationMinutes * 60,
            user);

        return Task.FromResult(Result.Success(response));
    }
}
