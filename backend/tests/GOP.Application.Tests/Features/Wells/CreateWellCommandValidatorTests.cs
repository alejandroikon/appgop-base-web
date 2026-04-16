using FluentAssertions;
using FluentValidation.TestHelper;
using GOP.Application.Features.Wells.Commands.CreateWell;

namespace GOP.Application.Tests.Features.Wells;

public sealed class CreateWellCommandValidatorTests
{
    private readonly CreateWellCommandValidator _validator = new();

    private static CreateWellCommand ValidCommand() => new(
        ContratoId: 1,
        CampoId: 1,
        TipoTrayectoria: "ST",
        Clasificacion: "EXPLORATORIO",
        Denominacion: "ALPHA",
        Consecutivo: "01",
        TipoUbicacion: "CONTINENTAL",
        TipoAngulo: "V",
        TipoObjetivo: "PH",
        TipoTerminacion: "CD",
        DepartamentoId: 1,
        MunicipioId: 1,
        ClusterId: null
    );

    [Fact]
    public void Validate_ValidInput_PassesValidation()
    {
        // Arrange
        var command = ValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyDenominacion_ReturnsError()
    {
        // Arrange
        var command = ValidCommand() with { Denominacion = "" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Denominacion)
            .WithErrorMessage("La denominación es requerida.");
    }

    [Fact]
    public void Validate_DenominacionWithNumbers_ReturnsError()
    {
        // Arrange
        var command = ValidCommand() with { Denominacion = "ALPHA123" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Denominacion)
            .WithErrorMessage("La denominación solo puede contener letras y espacios.");
    }

    [Fact]
    public void Validate_InvalidConsecutivo_ReturnsError()
    {
        // Arrange
        var command = ValidCommand() with { Consecutivo = "1" }; // Solo 1 dígito, debe ser 2

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Consecutivo)
            .WithErrorMessage("El consecutivo debe ser exactamente 2 dígitos numéricos.");
    }
}
