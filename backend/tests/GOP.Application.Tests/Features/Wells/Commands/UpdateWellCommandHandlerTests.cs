using FluentAssertions;
using GOP.Application.Features.Wells.Commands.UpdateWell;
using GOP.Application.Tests.Common;
using GOP.Domain.Entities;
using GOP.Domain.Enums;
using GOP.Domain.Errors;
using GOP.Domain.Interfaces;
using NSubstitute;

namespace GOP.Application.Tests.Features.Wells.Commands;

public sealed class UpdateWellCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static WellLocation BaseLocation() => new()
    {
        DepartamentoId = 1,
        MunicipioId = 1,
        CodigoDaneDpto = "50",
        CodigoDaneMpio = "50568"
    };

    private static Well BuildBorradorWell() => Well.Create(
        operadora: "Ecopetrol S.A.",
        tenantId: 1,
        contratoId: 1,
        tipoContrato: "E&P",
        cuenca: "Llanos Orientales",
        campoId: 1,
        tipoTrayectoria: TipoTrayectoria.ST,
        clasificacion: Clasificacion.Exploratorio,
        denominacion: "ALPHA",
        consecutivo: "01",
        tipoUbicacion: TipoUbicacion.Continental,
        tipoAngulo: TipoAngulo.V,
        tipoObjetivo: TipoObjetivo.PH,
        tipoTerminacion: TipoTerminacion.OH,
        location: BaseLocation());

    private static UpdateWellCommand ValidCommand(Guid wellId) => new(
        WellId: wellId,
        ContratoId: 1,
        CampoId: 1,
        TipoTrayectoria: "P",
        Clasificacion: "DESARROLLO",
        Denominacion: "BETA",
        Consecutivo: "02",
        TipoUbicacion: "CONTINENTAL",
        TipoAngulo: "H",
        TipoObjetivo: "I",
        TipoTerminacion: "LC",
        DepartamentoId: 1,
        MunicipioId: 1,
        ClusterId: null);

    private async Task<TestDbContext> CreateContextWithData(Well well)
    {
        var db = TestDbContext.Create();
        db.Contratos.Add(new Contrato { Id = 1, Nombre = "Contrato E&P Llanos", Tipo = "E&P", Cuenca = "Llanos Orientales" });
        db.Campos.Add(new Campo { Id = 1, Nombre = "Campo Rubiales", ContratoId = 1 });
        db.Departamentos.Add(new Departamento { Id = 1, Nombre = "Meta", CodigoDane = "50" });
        db.Municipios.Add(new Municipio { Id = 1, Nombre = "Puerto Gaitán", DepartamentoId = 1, CodigoDane = "50568" });
        db.Wells.Add(well);
        await db.SaveChangesAsync();

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ci => db.SaveChangesAsync(ci.Arg<CancellationToken>()));

        return db;
    }

    [Fact]
    public async Task Handle_ValidCommand_WellEnBorrador_ReturnsUpdatedDetail()
    {
        // Arrange
        var well = BuildBorradorWell();
        var db = await CreateContextWithData(well);
        var sut = new UpdateWellCommandHandler(db, _unitOfWork);
        var command = ValidCommand(well.Id);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NombrePozo.Should().Be("Llanos Orientales-BETA-02");
        result.Value.TipoTrayectoria.Should().Be("P");
        result.Value.Clasificacion.Should().Be("Desarrollo");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WellNotFound_ReturnsFailure()
    {
        // Arrange
        var db = TestDbContext.Create();
        var sut = new UpdateWellCommandHandler(db, _unitOfWork);
        var command = ValidCommand(Guid.NewGuid());

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Well.NotFound");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WellNoEnBorrador_ReturnsInvalidStatusFailure()
    {
        // Arrange: crear pozo y transitarlo a PendingUwi
        var well = BuildBorradorWell();
        well.ApplyTransition(TransitionAction.Enviar, "ADMIN", null);
        var db = await CreateContextWithData(well);
        var sut = new UpdateWellCommandHandler(db, _unitOfWork);
        var command = ValidCommand(well.Id);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Well.InvalidStatus);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidEnumValue_ReturnsFailureWithoutException()
    {
        // Arrange: valor de enum inválido que bypassea el validator
        var well = BuildBorradorWell();
        var db = await CreateContextWithData(well);
        var sut = new UpdateWellCommandHandler(db, _unitOfWork);
        var command = ValidCommand(well.Id) with { TipoTrayectoria = "INVALID_VALUE" };

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert — nunca debe lanzar excepción, siempre Result.Failure
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Well.InvalidFieldValue");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
