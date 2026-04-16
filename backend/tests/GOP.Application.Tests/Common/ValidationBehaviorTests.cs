using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using GOP.Application.Common.Behaviors;
using GOP.Domain.Common;
using MediatR;
using NSubstitute;

namespace GOP.Application.Tests.Common;

// Record interno (no file-scoped) para que NSubstitute pueda crear proxies
internal sealed record TestCommand(string Value) : IRequest<Result>;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestCommand>>();
        var behavior = new ValidationBehavior<TestCommand, Result>(validators);
        var request = new TestCommand("valor");
        var nextCalled = false;

        RequestHandlerDelegate<Result> next = ct =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success());
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        // Arrange
        var validator = Substitute.For<IValidator<TestCommand>>();
        validator.ValidateAsync(
            Arg.Any<ValidationContext<TestCommand>>(),
            Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<TestCommand, Result>(new[] { validator });
        var request = new TestCommand("valor-valido");
        var nextCalled = false;

        RequestHandlerDelegate<Result> next = ct =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success());
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_InvalidRequest_ReturnsFailureWithoutCallingHandler()
    {
        // Arrange
        var validationFailures = new List<ValidationFailure>
        {
            new("Value", "El valor es requerido.")
        };

        var validator = Substitute.For<IValidator<TestCommand>>();
        validator.ValidateAsync(
            Arg.Any<ValidationContext<TestCommand>>(),
            Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(validationFailures));

        var behavior = new ValidationBehavior<TestCommand, Result>(new[] { validator });
        var request = new TestCommand(string.Empty);
        var nextCalled = false;

        RequestHandlerDelegate<Result> next = ct =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success());
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeFalse("el handler no debe ejecutarse si la validación falla");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Validation.Failed");
    }
}
