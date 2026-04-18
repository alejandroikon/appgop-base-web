using FluentAssertions;
using GOP.Application.Features.Wells.Commands.TransitionWell;
using GOP.Application.Tests.Common;

namespace GOP.Application.Tests.Features.Wells.Commands;

// TODO-ITER-9.3: TransitionWell es legacy V1. El flujo V2.0 usa CreateWell(FINALIZE)/UpdateWell(FINALIZE).
public sealed class TransitionWellCommandHandlerTests
{
    [Fact]
    public async Task Handle_LegacyTransition_RetornaError()
    {
        var db = TestDbContext.Create();
        var sut = new TransitionWellCommandHandler(db);

        var result = await sut.Handle(new TransitionWellCommand(Guid.NewGuid(), "ENVIAR", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Well.NotFound");
    }
}
