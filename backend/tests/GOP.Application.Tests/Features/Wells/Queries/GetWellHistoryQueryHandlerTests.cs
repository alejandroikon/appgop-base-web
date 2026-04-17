using AutoMapper;
using FluentAssertions;
using GOP.Application.Features.Wells.Mappings;
using GOP.Application.Features.Wells.Queries.GetWellHistory;
using GOP.Application.Tests.Common;
using GOP.Domain.Entities;
using GOP.Domain.Enums;
using GOP.Domain.Errors;

namespace GOP.Application.Tests.Features.Wells.Queries;

public sealed class GetWellHistoryQueryHandlerTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(cfg =>
        cfg.AddProfile<WellTransitionMappingProfile>()).CreateMapper();

    private static Well BuildWell()
    {
        var location = new WellLocation
        {
            DepartamentoId = 1,
            MunicipioId = 1,
            CodigoDaneDpto = "50",
            CodigoDaneMpio = "50568"
        };

        return Well.Create(
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
    }

    [Fact]
    public async Task Handle_WellWithHistory_ReturnsOrderedList()
    {
        // Arrange
        var db = TestDbContext.Create();
        var well = BuildWell();
        db.Wells.Add(well);

        db.WellTransitionHistory.Add(WellTransitionHistory.Create(
            well.Id, WellStatus.Borrador, WellStatus.PendingUwi,
            TransitionAction.Enviar, null,
            Guid.NewGuid(), "Juan Pérez", "OPERADOR"));

        db.WellTransitionHistory.Add(WellTransitionHistory.Create(
            well.Id, WellStatus.PendingUwi, WellStatus.Borrador,
            TransitionAction.Devolver, "Revisar denominación.",
            Guid.NewGuid(), "María Gómez", "SUPERVISOR"));

        await db.SaveChangesAsync();

        var sut = new GetWellHistoryQueryHandler(db, Mapper);
        var query = new GetWellHistoryQuery(well.Id);

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        // Verificar que hay al menos un item con acción Devolver y uno con Enviar
        result.Value.Select(h => h.Action).Should().Contain("Devolver");
        result.Value.Select(h => h.Action).Should().Contain("Enviar");
    }

    [Fact]
    public async Task Handle_WellWithoutHistory_ReturnsEmptyList()
    {
        // Arrange
        var db = TestDbContext.Create();
        var well = BuildWell();
        db.Wells.Add(well);
        await db.SaveChangesAsync();

        var sut = new GetWellHistoryQueryHandler(db, Mapper);
        var query = new GetWellHistoryQuery(well.Id);

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WellNotFound_ReturnsNotFound()
    {
        // Arrange — ID que no existe en la DB
        var db = TestDbContext.Create();
        var sut = new GetWellHistoryQueryHandler(db, Mapper);
        var query = new GetWellHistoryQuery(Guid.NewGuid());

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Well.NotFound");
    }
}
