using FluentAssertions;
using GOP.Application.Features.Wells.Commands.DeleteWell;
using GOP.Application.Tests.Common;
using GOP.Domain.Entities;
using GOP.Domain.Enums;
using GOP.Domain.Errors;
using GOP.Domain.Interfaces;
using NSubstitute;

namespace GOP.Application.Tests.Features.Wells.Commands;

public sealed class DeleteWellCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static Well BuildBorradorWell() => Well.CreateDraft(
        operadora: "Ecopetrol S.A.",
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

    private async Task<TestDbContext> CreateContextWithWell(Well well)
    {
        var db = TestDbContext.Create();
        db.Wells.Add(well);
        await db.SaveChangesAsync();

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ci => db.SaveChangesAsync(ci.Arg<CancellationToken>()));

        return db;
    }

    [Fact]
    public async Task Handle_WellEnBorrador_SoftDeleteExitoso()
    {
        var well = BuildBorradorWell();
        var db = await CreateContextWithWell(well);
        var sut = new DeleteWellCommandHandler(db, _unitOfWork);

        var result = await sut.Handle(new DeleteWellCommand(well.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WellNotFound_RetornaFailure()
    {
        var db = TestDbContext.Create();
        var sut = new DeleteWellCommandHandler(db, _unitOfWork);

        var result = await sut.Handle(new DeleteWellCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Well.NotFound");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Forma101Radicada_NoPermiteEliminar()
    {
        // RN-40: pozo CREADO con Forma 101 no puede eliminarse
        var well = Well.CreateFinalized(
            operadora: "Ecopetrol S.A.", tenantId: 1, contratoId: 1, contrato: "E&P",
            tipoContrato: "E&P", cuenca: "Llanos Orientales", campoId: null, campo: null,
            denominacion: "ALPHA", consecutivo: 1, tipoTrayectoria: TipoTrayectoria.O,
            clasificacion: Clasificacion.Exploratorio, subClasificacion: null,
            tipoUbicacion: TipoUbicacion.Continental, tipoAngulo: TipoAngulo.V,
            tipoObjetivo: TipoObjetivo.PH, tipoTerminacion: TipoTerminacion.OH,
            departamentoId: 1, departamento: "Meta", codigoDaneDpto: "50",
            municipioId: 1, municipio: "Puerto Gaitán", codigoDaneMpio: "568",
            clusterId: null, cluster: null, uwi: "50568ALPH0001CX0000VPH-OH");
        well.MarkForma101Radicada();
        var db = await CreateContextWithWell(well);
        var sut = new DeleteWellCommandHandler(db, _unitOfWork);

        var result = await sut.Handle(new DeleteWellCommand(well.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Well.NotDeletable");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
