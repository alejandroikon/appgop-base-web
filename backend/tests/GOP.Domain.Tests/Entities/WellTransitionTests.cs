using FluentAssertions;
using GOP.Domain.Entities;
using GOP.Domain.Enums;

namespace GOP.Domain.Tests.Entities;

// TODO-ITER-9.3: Estos tests corresponden al flujo V1 (máquina de estados con TransitionAction).
// Se conservan como documentación. El flujo V2.0 está cubierto por WellTests.cs.
public sealed class WellTransitionTests
{
    [Fact]
    public void WellTransitionHistory_Legacy_Placeholder()
    {
        // El flujo V1 fue reemplazado en V2.0 por DRAFT/FINALIZE.
        // Este test existe para mantener el archivo compilable.
        true.Should().BeTrue();
    }
}
