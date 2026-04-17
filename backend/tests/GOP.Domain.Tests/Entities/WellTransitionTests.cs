using FluentAssertions;
using GOP.Domain.Entities;
using GOP.Domain.Enums;
using GOP.Domain.Errors;

namespace GOP.Domain.Tests.Entities;

public sealed class WellTransitionTests
{
    // ─── Helper para crear un Well en el estado deseado ──────────────────────

    private static Well CreateWell(WellStatus estado = WellStatus.Borrador)
    {
        var location = new WellLocation
        {
            DepartamentoId = 1,
            MunicipioId = 1,
            CodigoDaneDpto = "50",
            CodigoDaneMpio = "50568"
        };

        var well = Well.Create(
            operadora: "Ecopetrol",
            tenantId: 1,
            contratoId: 1,
            tipoContrato: "E&P",
            cuenca: "LLANOS",
            campoId: 1,
            tipoTrayectoria: TipoTrayectoria.ST,
            clasificacion: Clasificacion.Exploratorio,
            denominacion: "ALPHA",
            consecutivo: "01",
            tipoUbicacion: TipoUbicacion.Continental,
            tipoAngulo: TipoAngulo.V,
            tipoObjetivo: TipoObjetivo.PH,
            tipoTerminacion: TipoTerminacion.OH,
            location: location);

        // Avanzar al estado deseado mediante transiciones reales
        if (estado >= WellStatus.PendingUwi)
        {
            well.SetUwi("CO-50-50568-ALPHA-01-ST");
            well.ApplyTransition(TransitionAction.Enviar, "OPERADOR", null);
        }
        if (estado >= WellStatus.ReadyFiscal)
            well.ApplyTransition(TransitionAction.AprobarUwi, "SUPERVISOR", null);
        if (estado >= WellStatus.Fiscalizado)
            well.ApplyTransition(TransitionAction.Fiscalizar, "SUPERVISOR", null);

        return well;
    }

    // ─── CanTransition Tests ──────────────────────────────────────────────────

    [Fact]
    public void CanTransition_BorradorEnviar_ReturnsSuccess()
    {
        // Arrange
        var well = CreateWell(WellStatus.Borrador);

        // Act
        var result = well.CanTransition(TransitionAction.Enviar, "OPERADOR");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CanTransition_PendingUwiAprobarUwi_ReturnsSuccess()
    {
        // Arrange
        var well = CreateWell(WellStatus.PendingUwi);

        // Act
        var result = well.CanTransition(TransitionAction.AprobarUwi, "SUPERVISOR");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CanTransition_PendingUwiEnviar_ReturnsInvalidTransition()
    {
        // Arrange — ENVIAR no es válido desde PENDING_UWI
        var well = CreateWell(WellStatus.PendingUwi);

        // Act
        var result = well.CanTransition(TransitionAction.Enviar, "OPERADOR");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Well.InvalidTransition");
    }

    [Fact]
    public void CanTransition_OperadorAprobarUwi_ReturnsUnauthorized()
    {
        // Arrange — OPERADOR no puede APROBAR_UWI
        var well = CreateWell(WellStatus.PendingUwi);

        // Act
        var result = well.CanTransition(TransitionAction.AprobarUwi, "OPERADOR");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Well.TransitionUnauthorized);
    }

    [Fact]
    public void CanTransition_AuditorEnviar_ReturnsUnauthorized()
    {
        // Arrange — AUDITOR no tiene ningún rol de escritura
        var well = CreateWell(WellStatus.Borrador);

        // Act
        var result = well.CanTransition(TransitionAction.Enviar, "AUDITOR");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Well.TransitionUnauthorized);
    }

    [Fact]
    public void CanTransition_FiscalizadoAnyAction_ReturnsFailure()
    {
        // Arrange — Fiscalizado es estado terminal
        var well = CreateWell(WellStatus.Fiscalizado);

        // Act — intentar cualquier acción desde Fiscalizado
        var result = well.CanTransition(TransitionAction.Devolver, "ADMIN");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Well.FiscalizedImmutable);
    }

    // ─── ApplyTransition Tests ────────────────────────────────────────────────

    [Fact]
    public void ApplyTransition_DevolverWithComment_ChangesStateToBorrador()
    {
        // Arrange
        var well = CreateWell(WellStatus.PendingUwi);

        // Act
        var result = well.ApplyTransition(
            TransitionAction.Devolver, "SUPERVISOR", "Denominación incorrecta.");

        // Assert
        result.IsSuccess.Should().BeTrue();
        well.Estado.Should().Be(WellStatus.Borrador);
    }

    [Fact]
    public void ApplyTransition_DevolverWithoutComment_ReturnsCommentRequired()
    {
        // Arrange
        var well = CreateWell(WellStatus.PendingUwi);

        // Act
        var result = well.ApplyTransition(TransitionAction.Devolver, "SUPERVISOR", null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Well.CommentRequired);
        well.Estado.Should().Be(WellStatus.PendingUwi); // Estado no debe cambiar
    }
}
