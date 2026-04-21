using GOP.Application.Common.Interfaces;
using GOP.Application.Features.Auth.Dtos;
using GOP.Domain.Common;
using GOP.Domain.Errors;
using GOP.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Options;
using DomainRefreshToken = GOP.Domain.Entities.RefreshToken;

namespace GOP.Application.Features.Auth.Commands.RefreshToken;

internal sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository rtRepo,
    IUserRepository userRepo,
    IUserClaimsResolver claimsResolver,
    IJwtTokenService jwtTokenService,
    IOptions<JwtTokenOptions> jwtOptions,
    IUnitOfWork unitOfWork
) : IRequestHandler<RefreshTokenCommand, Result<TokenResponseDto>>
{
    public async Task<Result<TokenResponseDto>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Buscar el refresh token con tracking (necesario para mutar vía Revoke)
        var oldRt = await rtRepo.GetByTokenAsync(request.RefreshToken, cancellationToken);

        // 2. Null → inválido
        if (oldRt is null)
            return Result.Failure<TokenResponseDto>(DomainErrors.Auth.InvalidRefreshToken);

        // 3. Verificar si está activo; distinguir revocado vs expirado
        if (!oldRt.IsActive)
        {
            var domainError = oldRt.RevokedAt is not null
                ? DomainErrors.Auth.InvalidRefreshToken
                : DomainErrors.Auth.TokenExpired;
            return Result.Failure<TokenResponseDto>(domainError);
        }

        // 4-5. Buscar usuario y verificar que esté activo
        var user = await userRepo.GetByIdAsync(oldRt.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            return Result.Failure<TokenResponseDto>(DomainErrors.Auth.InvalidRefreshToken);

        // 6. Resolver claims de perfil
        var profile = claimsResolver.BuildProfile(user);

        // 7-8. Generar nuevo par de tokens
        var newAccessToken = jwtTokenService.GenerateAccessToken(profile);
        var newRefreshTokenStr = jwtTokenService.GenerateRefreshToken();

        var newExpiresAt = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationDays);
        var newRt = DomainRefreshToken.Create(user.Id, newRefreshTokenStr, newExpiresAt);
        await rtRepo.AddAsync(newRt, cancellationToken);

        // 9. Revocar el token viejo (entidad trackeada → SaveChanges lo persiste)
        oldRt.Revoke(replacedByToken: newRt.Token);

        // 10. Persistir ambos cambios en una única transacción
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 11. Retornar
        return Result.Success(new TokenResponseDto(
            newAccessToken,
            newRefreshTokenStr,
            jwtOptions.Value.AccessTokenExpirationMinutes * 60,
            profile));
    }
}
