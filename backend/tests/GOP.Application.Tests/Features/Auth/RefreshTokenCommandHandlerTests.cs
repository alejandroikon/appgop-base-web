using FluentAssertions;
using GOP.Application.Common.Interfaces;
using GOP.Application.Features.Auth.Commands.RefreshToken;
using GOP.Application.Features.Auth.Queries.GetCurrentUser;
using GOP.Domain.Entities;
using GOP.Domain.Errors;
using GOP.Domain.Interfaces;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GOP.Application.Tests.Features.Auth;

public sealed class RefreshTokenCommandHandlerTests
{
    private readonly IRefreshTokenRepository _rtRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IUserClaimsResolver _claimsResolver = Substitute.For<IUserClaimsResolver>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IOptions<JwtTokenOptions> _jwtOptions =
        Options.Create(new JwtTokenOptions { AccessTokenExpirationMinutes = 30, RefreshTokenExpirationDays = 7 });

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly UserProfileDto TestProfile = new(
        UserId, "test@gop.co", "Test User", "ADMIN", "1", "Tenant");

    private RefreshTokenCommandHandler CreateSut() =>
        new(_rtRepo, _userRepo, _claimsResolver, _jwtTokenService, _jwtOptions, _unitOfWork);

    [Fact]
    public async Task Handle_ValidRefreshToken_ReturnsNewTokenPair()
    {
        // Arrange
        var token = "valid-refresh-token";
        var rt = RefreshToken.Create(UserId, token, DateTime.UtcNow.AddDays(7));
        var user = User.Create("test@gop.co", "hash", "Test User");

        _rtRepo.GetByTokenAsync(token, Arg.Any<CancellationToken>()).Returns(rt);
        _userRepo.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _claimsResolver.BuildProfile(user).Returns(TestProfile);
        _jwtTokenService.GenerateAccessToken(TestProfile).Returns("new-access-token");
        _jwtTokenService.GenerateRefreshToken().Returns("new-refresh-token");

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new RefreshTokenCommand(token), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new-access-token");
        result.Value.RefreshToken.Should().Be("new-refresh-token");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        rt.RevokedAt.Should().NotBeNull("el token original debe ser revocado tras el refresh");
    }

    [Fact]
    public async Task Handle_ExpiredRefreshToken_ReturnsFailure()
    {
        // Arrange — ExpiresAt en el pasado → IsActive = false, RevokedAt = null → TokenExpired
        var token = "expired-token";
        var rt = RefreshToken.Create(UserId, token, DateTime.UtcNow.AddDays(-1));
        _rtRepo.GetByTokenAsync(token, Arg.Any<CancellationToken>()).Returns(rt);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new RefreshTokenCommand(token), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Auth.TokenExpired);
    }

    [Fact]
    public async Task Handle_InvalidRefreshToken_ReturnsFailure()
    {
        // Arrange — token inexistente
        var token = "nonexistent-token";
        _rtRepo.GetByTokenAsync(token, Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new RefreshTokenCommand(token), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Auth.InvalidRefreshToken);
    }

    [Fact]
    public async Task Handle_AlreadyUsedRefreshToken_ReturnsFailure()
    {
        // Arrange — token revocado antes del test (RevokedAt != null → InvalidRefreshToken)
        var token = "already-used-token";
        var rt = RefreshToken.Create(UserId, token, DateTime.UtcNow.AddDays(7));
        rt.Revoke();

        _rtRepo.GetByTokenAsync(token, Arg.Any<CancellationToken>()).Returns(rt);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new RefreshTokenCommand(token), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Auth.InvalidRefreshToken);
    }

    /// <summary>
    /// Decisión emergente (T028): User.IsActive tiene setter privado y no existe Deactivate().
    /// No se modifica el dominio (B1 inmutable). Se usa reflection para invocar el setter privado.
    /// </summary>
    [Fact]
    public async Task Handle_InactiveUserRefresh_ReturnsFailure()
    {
        // Arrange — RT válido pero usuario inactivo
        var token = "valid-token-inactive-user";
        var rt = RefreshToken.Create(UserId, token, DateTime.UtcNow.AddDays(7));
        var user = User.Create("test@gop.co", "hash", "Test User");

        // Setter privado — se invoca via reflection sin modificar la entidad de dominio
        typeof(User)
            .GetProperty(nameof(User.IsActive))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(user, [false]);

        _rtRepo.GetByTokenAsync(token, Arg.Any<CancellationToken>()).Returns(rt);
        _userRepo.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new RefreshTokenCommand(token), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Auth.InvalidRefreshToken);
    }
}
