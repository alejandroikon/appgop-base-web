using FluentAssertions;
using GOP.Application.Common.Interfaces;
using GOP.Application.Features.Auth.Commands.Login;
using GOP.Application.Features.Auth.Queries.GetCurrentUser;
using GOP.Domain.Entities;
using GOP.Domain.Errors;
using GOP.Domain.Interfaces;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GOP.Application.Tests.Features.Auth;

public sealed class LoginCommandHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _rtRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserClaimsResolver _claimsResolver = Substitute.For<IUserClaimsResolver>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IOptions<JwtTokenOptions> _jwtOptions =
        Options.Create(new JwtTokenOptions { AccessTokenExpirationMinutes = 30, RefreshTokenExpirationDays = 7 });

    private static readonly UserProfileDto TestUserProfile = new(
        Guid.NewGuid(), "admin@gop.co", "Administrador ANH", "ADMIN", "1", "ANH");

    private LoginCommandHandler CreateSut() =>
        new(_userRepo, _rtRepo, _claimsResolver, _passwordHasher, _jwtTokenService, _jwtOptions, _unitOfWork);

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokenResponse()
    {
        // Arrange
        var user = User.Create("admin@gop.co", "some-hash", "Administrador ANH");
        var command = new LoginCommand("admin@gop.co", "Admin123*");

        _userRepo.GetByEmailAsync("admin@gop.co", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("Admin123*", "some-hash").Returns(true);
        _claimsResolver.BuildProfile(user).Returns(TestUserProfile);
        _jwtTokenService.GenerateAccessToken(TestUserProfile).Returns("access-token");
        _jwtTokenService.GenerateRefreshToken().Returns("refresh-token");

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.ExpiresIn.Should().Be(1800);
        result.Value.User.Should().Be(TestUserProfile);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidPassword_ReturnsFailure()
    {
        // Arrange
        var user = User.Create("admin@gop.co", "some-hash", "Administrador ANH");
        var command = new LoginCommand("admin@gop.co", "WrongPassword");

        _userRepo.GetByEmailAsync("admin@gop.co", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("WrongPassword", "some-hash").Returns(false);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Auth.InvalidCredentials);
        _jwtTokenService.DidNotReceive().GenerateAccessToken(Arg.Any<UserProfileDto>());
    }

    [Fact]
    public async Task Handle_NonexistentEmail_ReturnsFailure()
    {
        // Arrange
        var command = new LoginCommand("unknown@gop.co", "SomePassword");
        _userRepo.GetByEmailAsync("unknown@gop.co", Arg.Any<CancellationToken>()).Returns((User?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Auth.InvalidCredentials,
            because: "no se debe revelar si el email existe o no");
        _jwtTokenService.DidNotReceive().GenerateAccessToken(Arg.Any<UserProfileDto>());
    }

    [Fact]
    public async Task Handle_CaseInsensitiveEmail_ReturnsSuccess()
    {
        // Arrange
        var user = User.Create("admin@gop.co", "some-hash", "Administrador ANH");
        var command = new LoginCommand("ADMIN@GOP.CO", "Admin123*");

        _userRepo.GetByEmailAsync("admin@gop.co", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("Admin123*", "some-hash").Returns(true);
        _claimsResolver.BuildProfile(user).Returns(TestUserProfile);
        _jwtTokenService.GenerateAccessToken(TestUserProfile).Returns("access-token");
        _jwtTokenService.GenerateRefreshToken().Returns("refresh-token");

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _userRepo.Received(1).GetByEmailAsync("admin@gop.co", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Decisión emergente (T027): User.IsActive tiene setter privado y no existe Deactivate().
    /// No se modifica el dominio (B1 inmutable). Se usa reflection para invocar el setter privado.
    /// </summary>
    [Fact]
    public async Task Handle_InactiveUser_ReturnsFailure()
    {
        // Arrange
        var user = User.Create("admin@gop.co", "some-hash", "Administrador ANH");
        // Setter privado — se invoca via reflection sin modificar la entidad de dominio
        typeof(User)
            .GetProperty(nameof(User.IsActive))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(user, [false]);

        var command = new LoginCommand("admin@gop.co", "Admin123*");
        _userRepo.GetByEmailAsync("admin@gop.co", Arg.Any<CancellationToken>()).Returns(user);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Auth.InvalidCredentials);
        // El handler retorna antes de verificar la contraseña (RN-029)
        _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
    }
}
