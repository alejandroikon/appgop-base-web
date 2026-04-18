using FluentAssertions;
using GOP.Application.Features.Wells.Commands.CreateWell;
using GOP.Application.Tests.Common;
using GOP.Domain.Entities;
using GOP.Domain.Interfaces;
using GOP.Domain.Interfaces.Repositories;
using GOP.Domain.Interfaces.Services;
using NSubstitute;

namespace GOP.Application.Tests.Features.Wells.Commands;

public sealed class CreateWellCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IWellRepository _wellRepo = Substitute.For<IWellRepository>();

    public CreateWellCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(1);
        _currentUser.TenantName.Returns("Ecopetrol");
        _wellRepo.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _wellRepo.ExistsByUwiAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);
    }

    private async Task<TestDbContext> CreateDbWithCatalogs()
    {
        var db = TestDbContext.Create();
        db.Contratos.Add(new Contrato { Id = 1, Nombre = "E&P Llanos", Tipo = "E&P", Cuenca = "Llanos Orientales" });
        db.Campos.Add(new Campo { Id = 1, Nombre = "Rubiales", ContratoId = 1 });
        db.Departamentos.Add(new Departamento { Id = 1, Nombre = "Meta", CodigoDane = "50" });
        db.Municipios.Add(new Municipio { Id = 1, Nombre = "Puerto Gaitán", CodigoDane = "50568", DepartamentoId = 1 });
        await db.SaveChangesAsync();

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ci => db.SaveChangesAsync(ci.Arg<CancellationToken>()));

        return db;
    }

    private static CreateWellCommand FinalizeCommand(int? campoId = 1) => new(
        Action: "FINALIZE",
        ContratoId: 1,
        CampoId: campoId,
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
    public async Task Handle_Draft_CreaEnEstadoBorrador()
    {
        var db = await CreateDbWithCatalogs();
        var sut = new CreateWellCommandHandler(db, _wellRepo, _unitOfWork, _currentUser);

        var result = await sut.Handle(new CreateWellCommand(
            Action: "DRAFT", ContratoId: 1, CampoId: 1, Denominacion: "Alpha", Consecutivo: 1,
            TipoTrayectoria: "O", Clasificacion: "EXPLORATORIO", SubClasificacion: null,
            TipoUbicacion: "CONTINENTAL", TipoAngulo: "V", TipoObjetivo: "PH",
            TipoTerminacion: "OH", DepartamentoId: 1, MunicipioId: 1, ClusterId: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Estado.Should().Be("BORRADOR");
        result.Value.Uwi.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Finalize_CreaConUwi()
    {
        var db = await CreateDbWithCatalogs();
        var sut = new CreateWellCommandHandler(db, _wellRepo, _unitOfWork, _currentUser);

        var result = await sut.Handle(FinalizeCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Estado.Should().Be("CREADO");
        result.Value.Uwi.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_UwiDuplicado_Retorna409()
    {
        var db = await CreateDbWithCatalogs();
        _wellRepo.ExistsByUwiAsync(Arg.Any<string>(), null, Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = new CreateWellCommandHandler(db, _wellRepo, _unitOfWork, _currentUser);

        var result = await sut.Handle(FinalizeCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Well.DuplicateUwi");
    }

    [Fact]
    public async Task Handle_NombreDuplicado_Retorna409()
    {
        var db = await CreateDbWithCatalogs();
        _wellRepo.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<int>(), null, Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = new CreateWellCommandHandler(db, _wellRepo, _unitOfWork, _currentUser);

        var result = await sut.Handle(FinalizeCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Well.DuplicateName");
    }

    [Fact]
    public async Task Handle_AnhTenantEstratigrafico_Exito()
    {
        // ANH puede crear pozos estratigráficos
        _currentUser.TenantName.Returns("ANH");
        var db = await CreateDbWithCatalogs();
        var sut = new CreateWellCommandHandler(db, _wellRepo, _unitOfWork, _currentUser);

        var result = await sut.Handle(FinalizeCommand() with
        {
            Clasificacion = "ESTRATIGRAFICO",
            SubClasificacion = null
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AnhTenantNoEstratigrafico_Retorna422()
    {
        // RN-15: ANH solo puede crear estratigráficos
        _currentUser.TenantName.Returns("ANH");
        var db = await CreateDbWithCatalogs();
        var sut = new CreateWellCommandHandler(db, _wellRepo, _unitOfWork, _currentUser);

        var result = await sut.Handle(FinalizeCommand() with
        {
            Clasificacion = "EXPLORATORIO"
        }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Well.InvalidClasificacionForAnh");
    }

    [Fact]
    public async Task Handle_DesarrolloSinCampo_RetornaCampoRequired()
    {
        // RN-12
        var db = await CreateDbWithCatalogs();
        var sut = new CreateWellCommandHandler(db, _wellRepo, _unitOfWork, _currentUser);

        var result = await sut.Handle(FinalizeCommand(campoId: null) with
        {
            Clasificacion = "DESARROLLO"
        }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Well.CampoRequiredForDesarrollo");
    }
}
