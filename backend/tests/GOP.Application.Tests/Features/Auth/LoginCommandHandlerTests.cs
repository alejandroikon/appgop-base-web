using FluentAssertions;
using GOP.Application.Common.Interfaces;
using GOP.Application.Features.Auth.Commands.Login;
using GOP.Application.Features.Auth.Queries.GetCurrentUser;
using GOP.Domain.Errors;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GOP.Application.Tests.Features.Auth;

public sealed class LoginCommandHandlerTests
{
    private readonly IUserSeedStore _userStore = Substitute.For<IUserSeedStore>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly IRefreshTokenStore _refreshTokenStore = Substitute.For<IRefreshTokenStore>();
    private readonly IOptions<JwtTokenOptions> _jwtOptions =
        Options.Create(new JwtTokenOptions { AccessTokenExpirationMinutes = 30, RefreshTokenExpirationDays = 7 });

    private LoginCommandHandler CreateSut() =>
        new(_userStore, _jwtTokenService, _refreshTokenStore, _jwtOptions);

    private static readonly UserProfileDto TestUser = new(
        Guid.NewGuid(), "admin@gop.co", "Administrador ANH", "ADMIN", "1", "ANH");

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokenResponse()
    {
        // Arrange
        var command = new LoginCommand("admin@gop.co", "Admin123*");
        _userStore.FindByEmail("admin@gop.co").Returns(TestUser);
        _userStore.VerifyPassword(TestUser.Id, "Admin123*").Returns(true);
        _jwtTokenService.GenerateAccessToken(TestUser).Returns("access-token");
        _jwtTokenService.GenerateRefreshToken().Returns("refresh-token");

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.ExpiresIn.Should().Be(1800);
        result.Value.User.Should().Be(TestUser);
        _refreshTokenStore.Received(1).Store("refresh-token", TestUser.Id, Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Handle_InvalidPassword_ReturnsFailure()
    {
        // Arrange
        var command = new LoginCommand("admin@gop.co", "WrongPassword");
        _userStore.FindByEmail("admin@gop.co").Returns(TestUser);
        _userStore.VerifyPassword(TestUser.Id, "WrongPassword").Returns(false);

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
        _userStore.FindByEmail("unknown@gop.co").Returns((UserProfileDto?)null);

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
        var command = new LoginCommand("ADMIN@GOP.CO", "Admin123*");
        _userStore.FindByEmail("admin@gop.co").Returns(TestUser);
        _userStore.VerifyPassword(TestUser.Id, "Admin123*").Returns(true);
        _jwtTokenService.GenerateAccessToken(TestUser).Returns("access-token");
        _jwtTokenService.GenerateRefreshToken().Returns("refresh-token");

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _userStore.Received(1).FindByEmail("admin@gop.co");
    }
}
