using FluentAssertions;
using GOP.Application.Features.Wells.Commands.TransitionWell;
using GOP.Application.Tests.Common;
using GOP.Domain.Common;
using GOP.Domain.Entities;
using GOP.Domain.Enums;
using GOP.Domain.Errors;
using GOP.Domain.Interfaces;
using GOP.Domain.Interfaces.Services;
using NSubstitute;

namespace GOP.Application.Tests.Features.Wells.Commands;

public sealed class TransitionWellCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUwiGenerator _uwiGenerator = Substitute.For<IUwiGenerator>();

    public TransitionWellCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(1);
        _currentUser.TenantName.Returns("Ecopetrol S.A.");
        _currentUser.UserId.Returns(Guid.Parse("660e8400-e29b-41d4-a716-446655440000"));
        _currentUser.Name.Returns("Juan Pérez");
        _currentUser.Role.Returns("OPERADOR");
        _currentUser.IsAuthenticated.Returns(true);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _uwiGenerator.GenerateAsync(Arg.Any<Well>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success("CO-50-50568-ALPHA-01-ST"));
    }

    private static Well BuildWell(WellStatus estado = WellStatus.Borrador)
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

    private async Task<(TestDbContext db, TransitionWellCommandHandler sut)> CreateHandler(Well? well = null)
    {
        var db = TestDbContext.Create();

        if (well is not null)
        {
            db.Wells.Add(well);
            await db.SaveChangesAsync();
        }

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ci => db.SaveChangesAsync(ci.Arg<CancellationToken>()));

        var sut = new TransitionWellCommandHandler(db, _unitOfWork, _currentUser, _uwiGenerator);
        return (db, sut);
    }

    // ─── Happy paths ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_EnviarFromBorrador_ReturnsSuccessAndGeneratesUwi()
    {
        // Arrange
        var well = BuildWell(WellStatus.Borrador);
        var (_, sut) = await CreateHandler(well);
        var command = new TransitionWellCommand(well.Id, "ENVIAR", null);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Estado.Should().Be("PendingUwi");
        result.Value.EstadoAnterior.Should().Be("Borrador");
        result.Value.Uwi.Should().Be("CO-50-50568-ALPHA-01-ST");
        result.Value.Action.Should().Be("Enviar");
        await _uwiGenerator.Received(1).GenerateAsync(Arg.Any<Well>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AprobarUwiFromPendingUwi_ReturnsSuccess()
    {
        // Arrange — usuario es SUPERVISOR para esta operación
        _currentUser.Role.Returns("SUPERVISOR");
        var well = BuildWell(WellStatus.PendingUwi);
        var (_, sut) = await CreateHandler(well);
        var command = new TransitionWellCommand(well.Id, "APROBAR_UWI", null);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Estado.Should().Be("ReadyFiscal");
        result.Value.EstadoAnterior.Should().Be("PendingUwi");
    }

    [Fact]
    public async Task Handle_DevolverWithComment_ReturnsSuccessWithComment()
    {
        // Arrange
        _currentUser.Role.Returns("SUPERVISOR");
        var well = BuildWell(WellStatus.PendingUwi);
        var (_, sut) = await CreateHandler(well);
        var comment = "Denominación incorrecta, revisar nomenclatura.";
        var command = new TransitionWellCommand(well.Id, "DEVOLVER", comment);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Estado.Should().Be("Borrador");
        result.Value.Comment.Should().Be(comment);
    }

    [Fact]
    public async Task Handle_FiscalizarFromReadyFiscal_ReturnsSuccess()
    {
        // Arrange
        _currentUser.Role.Returns("SUPERVISOR");
        var well = BuildWell(WellStatus.ReadyFiscal);
        var (_, sut) = await CreateHandler(well);
        var command = new TransitionWellCommand(well.Id, "FISCALIZAR", null);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Estado.Should().Be("Fiscalizado");
    }

    // ─── Edge cases / errores ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_InvalidTransition_ReturnsConflict()
    {
        // Arrange — ENVIAR desde PENDING_UWI no es válido
        var well = BuildWell(WellStatus.PendingUwi);
        var (_, sut) = await CreateHandler(well);
        var command = new TransitionWellCommand(well.Id, "ENVIAR", null);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Well.InvalidTransition");
    }

    [Fact]
    public async Task Handle_WellNotFound_ReturnsNotFound()
    {
        // Arrange — ID inexistente
        var (_, sut) = await CreateHandler(well: null);
        var command = new TransitionWellCommand(Guid.NewGuid(), "ENVIAR", null);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Well.NotFound");
    }

    [Fact]
    public async Task Handle_DuplicateUwi_ReturnsConflict()
    {
        // Arrange — UWI generado ya existe en otro pozo
        var existingWell = BuildWell(WellStatus.PendingUwi); // tiene UWI "CO-50-50568-ALPHA-01-ST"
        var newWell = BuildWell(WellStatus.Borrador);

        var db = TestDbContext.Create();
        db.Wells.Add(existingWell);
        db.Wells.Add(newWell);
        await db.SaveChangesAsync();

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ci => db.SaveChangesAsync(ci.Arg<CancellationToken>()));

        var sut = new TransitionWellCommandHandler(db, _unitOfWork, _currentUser, _uwiGenerator);
        var command = new TransitionWellCommand(newWell.Id, "ENVIAR", null);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Well.DuplicateUwi);
    }

    [Fact]
    public async Task Handle_ResubmitWithExistingUwi_PreservesUwi()
    {
        // Arrange — pozo en BORRADOR que ya tiene UWI (post-devolución)
        var well = BuildWell(WellStatus.Borrador);
        well.SetUwi("CO-50-50568-ALPHA-01-ST");

        var (_, sut) = await CreateHandler(well);
        var command = new TransitionWellCommand(well.Id, "ENVIAR", null);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Uwi.Should().Be("CO-50-50568-ALPHA-01-ST");
        // No debe generar un nuevo UWI porque ya existe
        await _uwiGenerator.DidNotReceive().GenerateAsync(Arg.Any<Well>(), Arg.Any<CancellationToken>());
    }
}
