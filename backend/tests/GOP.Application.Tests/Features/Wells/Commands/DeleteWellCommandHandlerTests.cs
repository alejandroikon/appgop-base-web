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
        location: new WellLocation
        {
            DepartamentoId = 1,
            MunicipioId = 1,
            CodigoDaneDpto = "50",
            CodigoDaneMpio = "50568"
        });

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
    public async Task Handle_WellEnBorrador_ReturnsSoftDeleteSuccess()
    {
        // Arrange
        var well = BuildBorradorWell();
        var db = await CreateContextWithWell(well);
        var sut = new DeleteWellCommandHandler(db, _unitOfWork);

        // Act
        var result = await sut.Handle(new DeleteWellCommand(well.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var deleted = await db.Wells.FindAsync(well.Id);
        deleted!.IsDeleted.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WellNotFound_ReturnsFailure()
    {
        // Arrange
        var db = TestDbContext.Create();
        var sut = new DeleteWellCommandHandler(db, _unitOfWork);

        // Act
        var result = await sut.Handle(new DeleteWellCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Well.NotFound");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WellNoEnBorrador_ReturnsDeleteInvalidStatusFailure()
    {
        // Arrange: well transitado a PendingUwi (ya no es Borrador)
        var well = BuildBorradorWell();
        well.ApplyTransition(TransitionAction.Enviar, "ADMIN", null);
        var db = await CreateContextWithWell(well);
        var sut = new DeleteWellCommandHandler(db, _unitOfWork);

        // Act
        var result = await sut.Handle(new DeleteWellCommand(well.Id), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Well.DeleteInvalidStatus);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
