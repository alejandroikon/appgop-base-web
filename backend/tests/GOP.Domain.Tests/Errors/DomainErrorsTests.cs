using FluentAssertions;
using GOP.Domain.Errors;

namespace GOP.Domain.Tests.Errors;

public sealed class DomainErrorsTests
{
    [Fact]
    public void Auth_InvalidCredentials_HasCodeAndMessage()
    {
        // Arrange & Act
        var error = DomainErrors.Auth.InvalidCredentials;

        // Assert
        error.Code.Should().NotBeNullOrEmpty();
        error.Message.Should().NotBeNullOrEmpty();
        error.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public void Auth_TokenExpired_HasCodeAndMessage()
    {
        // Arrange & Act
        var error = DomainErrors.Auth.TokenExpired;

        // Assert
        error.Code.Should().NotBeNullOrEmpty();
        error.Message.Should().NotBeNullOrEmpty();
        error.Code.Should().Be("Auth.TokenExpired");
    }

    [Fact]
    public void Auth_InvalidRefreshToken_HasCodeAndMessage()
    {
        // Arrange & Act
        var error = DomainErrors.Auth.InvalidRefreshToken;

        // Assert
        error.Code.Should().NotBeNullOrEmpty();
        error.Message.Should().NotBeNullOrEmpty();
        error.Code.Should().Be("Auth.InvalidRefreshToken");
    }
}
