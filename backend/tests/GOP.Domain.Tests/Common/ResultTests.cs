using FluentAssertions;
using GOP.Domain.Common;

namespace GOP.Domain.Tests.Common;

public sealed class ResultTests
{
    [Fact]
    public void Success_ReturnsIsSuccessTrue()
    {
        // Arrange & Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_ReturnsIsFailureTrueWithError()
    {
        // Arrange
        var error = new Error("Test.Error", "Error de prueba.");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void SuccessT_ReturnsValueCorrectly()
    {
        // Arrange & Act
        var result = Result.Success(42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void FailureT_AccessingValueThrowsException()
    {
        // Arrange
        var error = new Error("Test.Error", "Error de prueba.");
        var result = Result.Failure<int>(error);

        // Act
        var act = () => result.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }
}
