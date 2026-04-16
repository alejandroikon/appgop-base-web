using FluentAssertions;
using GOP.Application.Features.Auth.Commands.Login;

namespace GOP.Application.Tests.Features.Auth;

public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public async Task Validate_EmptyEmail_ReturnsError()
    {
        // Arrange
        var command = new LoginCommand(string.Empty, "Admin123*");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_InvalidEmailFormat_ReturnsError()
    {
        // Arrange
        var command = new LoginCommand("not-an-email", "Admin123*");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_EmptyPassword_ReturnsError()
    {
        // Arrange
        var command = new LoginCommand("admin@gop.co", string.Empty);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Validate_ValidInput_PassesValidation()
    {
        // Arrange
        var command = new LoginCommand("admin@gop.co", "Admin123*");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
