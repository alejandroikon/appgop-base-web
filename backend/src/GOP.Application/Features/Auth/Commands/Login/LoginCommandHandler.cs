using GOP.Application.Common.Interfaces;
using GOP.Application.Features.Auth.Dtos;
using GOP.Domain.Common;
using GOP.Domain.Errors;
using GOP.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Options;
using DomainRefreshToken = GOP.Domain.Entities.RefreshToken;

namespace GOP.Application.Features.Auth.Commands.Login;

internal sealed class LoginCommandHandler(
    IUserRepository userRepo,
    IRefreshTokenRepository rtRepo,
    IUserClaimsResolver claimsResolver,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IOptions<JwtTokenOptions> jwtOptions,
    IUnitOfWork unitOfWork
) : IRequestHandler<LoginCommand, Result<TokenResponseDto>>
{
    public async Task<Result<TokenResponseDto>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Normalizar email
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 2. Buscar usuario por email
        var user = await userRepo.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null)
            return Result.Failure<TokenResponseDto>(DomainErrors.Auth.InvalidCredentials);

        // 3. Verificar usuario activo (RN-029)
        if (!user.IsActive)
            return Result.Failure<TokenResponseDto>(DomainErrors.Auth.InvalidCredentials);

        // 4. Verificar contraseña
        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result.Failure<TokenResponseDto>(DomainErrors.Auth.InvalidCredentials);

        // 5. Resolver claims de perfil (transitorio hasta Iter 9)
        var profile = claimsResolver.BuildProfile(user);

        // 6-7. Generar tokens
        var accessToken = jwtTokenService.GenerateAccessToken(profile);
        var refreshTokenStr = jwtTokenService.GenerateRefreshToken();

        // 8. Crear y persistir RefreshToken
        var expiresAt = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationDays);
        var rt = DomainRefreshToken.Create(user.Id, refreshTokenStr, expiresAt);
        await rtRepo.AddAsync(rt, cancellationToken);

        // 9. Registrar login en la entidad
        user.RecordLogin();

        // 10. Persistir ambos cambios en una única transacción
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 11. Retornar respuesta
        return Result.Success(new TokenResponseDto(
            accessToken,
            refreshTokenStr,
            jwtOptions.Value.AccessTokenExpirationMinutes * 60,
            profile));
    }
}
