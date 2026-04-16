using GOP.Application.Common.Interfaces;
using GOP.Application.Features.Auth.Dtos;
using GOP.Domain.Common;
using GOP.Domain.Errors;
using MediatR;
using Microsoft.Extensions.Options;

namespace GOP.Application.Features.Auth.Commands.RefreshToken;

internal sealed class RefreshTokenCommandHandler(
    IRefreshTokenStore refreshTokenStore,
    IUserSeedStore userStore,
    IJwtTokenService jwtTokenService,
    IOptions<JwtTokenOptions> jwtOptions
) : IRequestHandler<RefreshTokenCommand, Result<TokenResponseDto>>
{
    public Task<Result<TokenResponseDto>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var entry = refreshTokenStore.TryConsume(request.RefreshToken);

        if (entry is null)
            return Task.FromResult(Result.Failure<TokenResponseDto>(DomainErrors.Auth.InvalidRefreshToken));

        if (entry.IsUsed || entry.ExpiresAt < DateTime.UtcNow)
        {
            var domainError = entry.IsUsed
                ? DomainErrors.Auth.InvalidRefreshToken
                : DomainErrors.Auth.TokenExpired;
            return Task.FromResult(Result.Failure<TokenResponseDto>(domainError));
        }

        var user = userStore.GetById(entry.UserId);
        if (user is null)
            return Task.FromResult(Result.Failure<TokenResponseDto>(DomainErrors.Auth.InvalidRefreshToken));

        var newAccessToken = jwtTokenService.GenerateAccessToken(user);
        var newRefreshToken = jwtTokenService.GenerateRefreshToken();

        var expiresAt = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationDays);
        refreshTokenStore.Store(newRefreshToken, user.Id, expiresAt);

        var response = new TokenResponseDto(
            newAccessToken,
            newRefreshToken,
            jwtOptions.Value.AccessTokenExpirationMinutes * 60,
            user);

        return Task.FromResult(Result.Success(response));
    }
}
