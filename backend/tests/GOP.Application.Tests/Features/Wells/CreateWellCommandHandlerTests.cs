using FluentAssertions;
using GOP.Application.Features.Wells.Commands.CreateWell;
using GOP.Application.Tests.Common;
using GOP.Domain.Entities;
using GOP.Domain.Errors;
using GOP.Domain.Interfaces;
using GOP.Domain.Interfaces.Services;
using NSubstitute;

namespace GOP.Application.Tests.Features.Wells;

public sealed class CreateWellCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    public CreateWellCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(1);
        _currentUser.TenantName.Returns("Ecopetrol S.A.");
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Email.Returns("operador@test.co");
    }

    private static CreateWellCommand ValidCommand() => new(
        ContratoId: 1, CampoId: 1, TipoTrayectoria: "ST", Clasificacion: "EXPLORATORIO",
        Denominacion: "ALPHA", Consecutivo: "01", TipoUbicacion: "CONTINENTAL",
        TipoAngulo: "V", TipoObjetivo: "PH", TipoTerminacion: "CD",
        DepartamentoId: 1, MunicipioId: 1, ClusterId: null);

    private async Task<TestDbContext> CreateContextWithCatalogs()
    {
        var db = TestDbContext.Create();

        db.Contratos.Add(new Contrato { Id = 1, Nombre = "Contrato E&P Llanos", Tipo = "E&P", Cuenca = "Llanos Orientales" });
        db.Campos.Add(new Campo { Id = 1, Nombre = "Campo Rubiales", ContratoId = 1 });
        db.Departamentos.Add(new Departamento { Id = 1, Nombre = "Meta", CodigoDane = "50" });
        db.Municipios.Add(new Municipio { Id = 1, Nombre = "Puerto Gaitán", DepartamentoId = 1, CodigoDane = "50568" });
        await db.SaveChangesAsync();

        // Configurar el IUnitOfWork para delegar al TestDbContext
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ci => db.SaveChangesAsync(ci.Arg<CancellationToken>()));

        return db;
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsWellDetail()
    {
        // Arrange
        var db = await CreateContextWithCatalogs();
        var sut = new CreateWellCommandHandler(db, _unitOfWork, _currentUser);

        // Act
        var result = await sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NombrePozo.Should().Be("Llanos Orientales-ALPHA-01");
        result.Value.Operadora.Should().Be("Ecopetrol S.A.");
        result.Value.Estado.Should().Be("Borrador");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_CalculatesNombrePozoCorrectly()
    {
        // Arrange
        var db = await CreateContextWithCatalogs();
        var sut = new CreateWellCommandHandler(db, _unitOfWork, _currentUser);
        var command = ValidCommand() with { Denominacion = "BETA", Consecutivo = "02" };

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NombrePozo.Should().Be("Llanos Orientales-BETA-02");
    }

    [Fact]
    public async Task Handle_CampoNotBelongsToContrato_ReturnsFailure()
    {
        // Arrange
        var db = await CreateContextWithCatalogs();
        var sut = new CreateWellCommandHandler(db, _unitOfWork, _currentUser);
        var command = ValidCommand() with { CampoId = 99 }; // No pertenece al ContratoId=1

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Well.CampoNotBelongsToContrato);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MunicipioNotBelongsToDepartamento_ReturnsFailure()
    {
        // Arrange
        var db = await CreateContextWithCatalogs();
        var sut = new CreateWellCommandHandler(db, _unitOfWork, _currentUser);
        var command = ValidCommand() with { MunicipioId = 99 }; // No pertenece al DepartamentoId=1

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Well.MunicipioNotBelongsToDepartamento);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
