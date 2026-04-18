using FluentAssertions;
using FluentValidation.TestHelper;
using GOP.Application.Features.Wells.Commands.CreateWell;

namespace GOP.Application.Tests.Features.Wells;

public sealed class CreateWellCommandValidatorTests
{
    private readonly CreateWellCommandValidator _validator = new();

    private static CreateWellCommand ValidFinalizeCommand() => new(
        Action: "FINALIZE",
        ContratoId: 1,
        CampoId: 1,
        Denominacion: "Cusiana Renata",
        Consecutivo: 1,
        TipoTrayectoria: "O",
        Clasificacion: "EXPLORATORIO",
        SubClasificacion: "A3",
        TipoUbicacion: "CONTINENTAL",
        TipoAngulo: "V",
        TipoObjetivo: "PH",
        TipoTerminacion: "OH",
        DepartamentoId: 1,
        MunicipioId: 1,
        ClusterId: null);

    [Fact]
    public void Validate_ValidFinalizeCommand_SinErrores()
    {
        var result = _validator.TestValidate(ValidFinalizeCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ActionVacia_ReturnsError()
    {
        var command = ValidFinalizeCommand() with { Action = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Action);
    }

    [Fact]
    public void Validate_ActionInvalida_ReturnsError()
    {
        var command = ValidFinalizeCommand() with { Action = "INVALID" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Action);
    }

    [Fact]
    public void Validate_ConsecutivoFueraDeRango_ReturnsError()
    {
        var command = ValidFinalizeCommand() with { Consecutivo = 10000 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("Consecutivo.Value");
    }

    [Fact]
    public void Validate_DenominacionConCaracteresInvalidos_ReturnsError()
    {
        var command = ValidFinalizeCommand() with { Denominacion = "ALPHA@#$" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Denominacion);
    }

    [Fact]
    public void Validate_DraftConSoloPocosFields_OK()
    {
        var draft = new CreateWellCommand(
            Action: "DRAFT",
            ContratoId: 1,
            CampoId: null,
            Denominacion: "Test",
            Consecutivo: 1,
            TipoTrayectoria: null, Clasificacion: null, SubClasificacion: null,
            TipoUbicacion: null, TipoAngulo: null, TipoObjetivo: null,
            TipoTerminacion: null, DepartamentoId: null, MunicipioId: null, ClusterId: null);
        var result = _validator.TestValidate(draft);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
