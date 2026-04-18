using FluentAssertions;
using GOP.Application.Features.Wells.Commands.UpdateWell;
using GOP.Application.Tests.Common;
using GOP.Domain.Entities;
using GOP.Domain.Enums;
using GOP.Domain.Interfaces;
using GOP.Domain.Interfaces.Repositories;
using GOP.Domain.Interfaces.Services;
using NSubstitute;

namespace GOP.Application.Tests.Features.Wells.Commands;

public sealed class UpdateWellCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    public UpdateWellCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(1);
        _currentUser.TenantName.Returns("Ecopetrol");
    }

    private static Well BuildBorradorWell() => Well.CreateDraft(
        operadora: "Ecopetrol",
        tenantId: 1,
        contratoId: 1,
        contrato: "E&P Llanos",
        tipoContrato: "E&P",
        cuenca: "Llanos Orientales",
        campoId: 1,
        campo: "Rubiales",
        denominacion: "ALPHA",
        consecutivo: 1,
        tipoTrayectoria: TipoTrayectoria.O,
        clasificacion: Clasificacion.Exploratorio,
        subClasificacion: null,
        tipoUbicacion: TipoUbicacion.Continental,
        tipoAngulo: TipoAngulo.V,
        tipoObjetivo: TipoObjetivo.PH,
        tipoTerminacion: TipoTerminacion.OH,
        departamentoId: 1,
        departamento: "Meta",
        codigoDaneDpto: "50",
        municipioId: 1,
        municipio: "Puerto Gaitán",
        codigoDaneMpio: "568",
        clusterId: null,
        cluster: null);

    private async Task<(TestDbContext db, IWellRepository repo)> CreateContextWithWell(Well well)
    {
        var db = TestDbContext.Create();
        db.Contratos.Add(new Contrato { Id = 1, Nombre = "E&P Llanos", Tipo = "E&P", Cuenca = "Llanos Orientales" });
        db.Campos.Add(new Campo { Id = 1, Nombre = "Rubiales", ContratoId = 1 });
        db.Departamentos.Add(new Departamento { Id = 1, Nombre = "Meta", CodigoDane = "50" });
        db.Municipios.Add(new Municipio { Id = 1, Nombre = "Puerto Gaitán", CodigoDane = "50568", DepartamentoId = 1 });
        db.Wells.Add(well);
        await db.SaveChangesAsync();

        var repo = Substitute.For<IWellRepository>();
        repo.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        repo.ExistsByUwiAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        repo.When(r => r.Update(Arg.Any<Well>())).Do(ci => { }); // no-op

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ci => db.SaveChangesAsync(ci.Arg<CancellationToken>()));

        return (db, repo);
    }

    [Fact]
    public async Task Handle_BorradorSAVE_ActualizaExitosamente()
    {
        var well = BuildBorradorWell();
        var (db, repo) = await CreateContextWithWell(well);
        var sut = new UpdateWellCommandHandler(db, repo, _unitOfWork, _currentUser);

        var command = new UpdateWellCommand(
            WellId: well.Id, Action: "SAVE",
            ContratoId: 1, CampoId: 1, Denominacion: "BETA", Consecutivo: 2,
            TipoTrayectoria: "O", Clasificacion: "EXPLORATORIO", SubClasificacion: null,
            TipoUbicacion: "CONTINENTAL", TipoAngulo: "V", TipoObjetivo: "PH",
            TipoTerminacion: "OH", DepartamentoId: 1, MunicipioId: 1, ClusterId: null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Forma101Radicada_RetornaNotEditable()
    {
        // RN-40: pozo CREADO con Forma 101 no puede editarse
        var well = Well.CreateFinalized(
            operadora: "Ecopetrol", tenantId: 1, contratoId: 1, contrato: "E&P",
            tipoContrato: "E&P", cuenca: "Llanos Orientales", campoId: null, campo: null,
            denominacion: "ALPHA", consecutivo: 1, tipoTrayectoria: TipoTrayectoria.O,
            clasificacion: Clasificacion.Exploratorio, subClasificacion: null,
            tipoUbicacion: TipoUbicacion.Continental, tipoAngulo: TipoAngulo.V,
            tipoObjetivo: TipoObjetivo.PH, tipoTerminacion: TipoTerminacion.OH,
            departamentoId: 1, departamento: "Meta", codigoDaneDpto: "50",
            municipioId: 1, municipio: "Puerto Gaitán", codigoDaneMpio: "568",
            clusterId: null, cluster: null, uwi: "50568ALPH0001CX0000VPH-OH");
        well.MarkForma101Radicada();
        var (db, repo) = await CreateContextWithWell(well);
        var sut = new UpdateWellCommandHandler(db, repo, _unitOfWork, _currentUser);

        var command = new UpdateWellCommand(
            WellId: well.Id, Action: "SAVE",
            ContratoId: 1, CampoId: null, Denominacion: null, Consecutivo: null,
            TipoTrayectoria: null, Clasificacion: null, SubClasificacion: null,
            TipoUbicacion: null, TipoAngulo: null, TipoObjetivo: null,
            TipoTerminacion: null, DepartamentoId: null, MunicipioId: null, ClusterId: null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Well.NotEditable");
    }

    [Fact]
    public async Task Handle_NotFound_RetornaFailure()
    {
        var db = TestDbContext.Create();
        var repo = Substitute.For<IWellRepository>();
        var sut = new UpdateWellCommandHandler(db, repo, _unitOfWork, _currentUser);

        var command = new UpdateWellCommand(
            WellId: Guid.NewGuid(), Action: "SAVE",
            ContratoId: null, CampoId: null, Denominacion: null, Consecutivo: null,
            TipoTrayectoria: null, Clasificacion: null, SubClasificacion: null,
            TipoUbicacion: null, TipoAngulo: null, TipoObjetivo: null,
            TipoTerminacion: null, DepartamentoId: null, MunicipioId: null, ClusterId: null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Well.NotFound");
    }

    [Fact]
    public async Task Handle_FinalizeBorrador_GeneraUwiYCambiaACreado()
    {
        var well = BuildBorradorWell();
        var (db, repo) = await CreateContextWithWell(well);
        var sut = new UpdateWellCommandHandler(db, repo, _unitOfWork, _currentUser);

        var command = new UpdateWellCommand(
            WellId: well.Id, Action: "FINALIZE",
            ContratoId: 1, CampoId: 1, Denominacion: "ALPHA", Consecutivo: 1,
            TipoTrayectoria: "O", Clasificacion: "EXPLORATORIO", SubClasificacion: null,
            TipoUbicacion: "CONTINENTAL", TipoAngulo: "V", TipoObjetivo: "PH",
            TipoTerminacion: "OH", DepartamentoId: 1, MunicipioId: 1, ClusterId: null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Estado.Should().Be("CREADO");
        result.Value.Uwi.Should().NotBeNullOrEmpty();
    }
}
