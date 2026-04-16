using FluentAssertions;
using GOP.Application.Common.Interfaces;
using GOP.Application.Features.Auth.Commands.RefreshToken;
using GOP.Application.Features.Auth.Queries.GetCurrentUser;
using GOP.Domain.Errors;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GOP.Application.Tests.Features.Auth;

public sealed class RefreshTokenCommandHandlerTests
{
    private readonly IRefreshTokenStore _refreshTokenStore = Substitute.For<IRefreshTokenStore>();
    private readonly IUserSeedStore _userStore = Substitute.For<IUserSeedStore>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly IOptions<JwtTokenOptions> _jwtOptions =
        Options.Create(new JwtTokenOptions { AccessTokenExpirationMinutes = 30, RefreshTokenExpirationDays = 7 });

    private RefreshTokenCommandHandler CreateSut() =>
        new(_refreshTokenStore, _userStore, _jwtTokenService, _jwtOptions);

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly UserProfileDto TestUser = new(
        UserId, "test@gop.co", "Test User", "ADMIN", "1", "Tenant");

    [Fact]
    public async Task Handle_ValidRefreshToken_ReturnsNewTokenPair()
    {
        // Arrange
        var token = "valid-refresh-token";
        var entry = new RefreshTokenEntry(
            UserId, token, DateTime.UtcNow.AddDays(7), IsUsed: false);

        _refreshTokenStore.TryConsume(token).Returns(entry);
        _userStore.GetById(UserId).Returns(TestUser);
        _jwtTokenService.GenerateAccessToken(TestUser).Returns("new-access-token");
        _jwtTokenService.GenerateRefreshToken().Returns("new-refresh-token");

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new RefreshTokenCommand(token), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new-access-token");
        result.Value.RefreshToken.Should().Be("new-refresh-token");
        _refreshTokenStore.Received(1).Store("new-refresh-token", UserId, Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Handle_ExpiredRefreshToken_ReturnsFailure()
    {
        // Arrange
        var token = "expired-token";
        var entry = new RefreshTokenEntry(
            UserId, token, DateTime.UtcNow.AddDays(-1), IsUsed: false);

        _refreshTokenStore.TryConsume(token).Returns(entry);

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
        // Arrange
        var token = "nonexistent-token";
        _refreshTokenStore.TryConsume(token).Returns((RefreshTokenEntry?)null);

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
        // Arrange
        var token = "already-used-token";
        var entry = new RefreshTokenEntry(
            UserId, token, DateTime.UtcNow.AddDays(7), IsUsed: true);

        _refreshTokenStore.TryConsume(token).Returns(entry);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new RefreshTokenCommand(token), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Auth.InvalidRefreshToken);
    }
}
