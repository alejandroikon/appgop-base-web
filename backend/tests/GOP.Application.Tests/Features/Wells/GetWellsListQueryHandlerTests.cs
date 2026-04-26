using FluentAssertions;
using GOP.Application.Features.Wells.Queries.GetWellsList;
using GOP.Application.Tests.Common;
using GOP.Domain.Entities;
using GOP.Domain.Enums;
using GOP.Domain.Interfaces.Services;
using NSubstitute;

namespace GOP.Application.Tests.Features.Wells;

public sealed class GetWellsListQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    public GetWellsListQueryHandlerTests()
    {
        _currentUser.IsInRole("ADMIN").Returns(false);
        _currentUser.IsInRole("AUDITOR").Returns(false);
        _currentUser.TenantId.Returns(1);
    }

    private static Well BuildWell(int tenantId = 1, int contratoId = 1, string denominacion = "ALPHA", int campoId = 1, string campo = "Rubiales")
        => Well.CreateDraft(
            operadora: "Ecopetrol", tenantId: tenantId, contratoId: contratoId,
            contrato: "E&P Llanos", tipoContrato: "E&P", cuenca: "Llanos",
            campoId: campoId, campo: campo, denominacion: denominacion, consecutivo: 1,
            tipoTrayectoria: TipoTrayectoria.O, clasificacion: Clasificacion.Exploratorio,
            subClasificacion: null, tipoUbicacion: TipoUbicacion.Continental,
            tipoAngulo: TipoAngulo.V, tipoObjetivo: TipoObjetivo.PH, tipoTerminacion: TipoTerminacion.OH,
            departamentoId: 1, departamento: "Meta", codigoDaneDpto: "50",
            municipioId: 1, municipio: "Puerto Gaitán", codigoDaneMpio: "568",
            clusterId: null, cluster: null);

    private async Task<TestDbContext> CreateContextWithWells(List<Well> wells)
    {
        var db = TestDbContext.Create();
        db.Contratos.Add(new Contrato { Id = 1, Nombre = "Contrato Llanos", Tipo = "E&P", Cuenca = "Llanos" });
        db.Wells.AddRange(wells);
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task Handle_SinFiltros_RetornaTodasLasPagina()
    {
        var wells = Enumerable.Range(1, 5).Select(i => BuildWell(denominacion: $"WELL{i}")).ToList();
        var db = await CreateContextWithWells(wells);
        var sut = new GetWellsListQueryHandler(db, _currentUser);

        var result = await sut.Handle(new GetWellsListQuery(1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(5);
    }

    [Fact]
    public async Task Handle_ConFiltroContratoId_FiltraCorrectamente()
    {
        var wells = new List<Well>
        {
            BuildWell(contratoId: 1, denominacion: "ALPHA"),
            BuildWell(contratoId: 2, denominacion: "BETA")
        };
        var db = TestDbContext.Create();
        db.Contratos.Add(new Contrato { Id = 1, Nombre = "Contrato 1", Tipo = "E&P", Cuenca = "Llanos" });
        db.Contratos.Add(new Contrato { Id = 2, Nombre = "Contrato 2", Tipo = "E&P", Cuenca = "Piedemonte" });
        db.Wells.AddRange(wells);
        await db.SaveChangesAsync();
        var sut = new GetWellsListQueryHandler(db, _currentUser);

        var result = await sut.Handle(new GetWellsListQuery(1, 20, ContratoId: 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(1);
        result.Value.Items.First().NombrePozo.Should().Contain("ALPHA");
    }

    [Fact]
    public async Task Handle_Paginacion_RetornaSoloItemsDeLaPagina()
    {
        var wells = Enumerable.Range(1, 10).Select(i => BuildWell(denominacion: $"WELL{i:D2}")).ToList();
        var db = await CreateContextWithWells(wells);
        var sut = new GetWellsListQueryHandler(db, _currentUser);

        var result = await sut.Handle(new GetWellsListQuery(1, 3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Count.Should().Be(3);
        result.Value.Total.Should().Be(10);
    }

    [Fact]
    public async Task Handle_WithCampoIdFilter_FiltersCorrectly()
    {
        // Arrange — 2 pozos campoId=1, 1 pozo campoId=2
        var wells = new List<Well>
        {
            BuildWell(denominacion: "CAMPO1A", campoId: 1, campo: "Rubiales"),
            BuildWell(denominacion: "CAMPO1B", campoId: 1, campo: "Rubiales"),
            BuildWell(denominacion: "CAMPO2A", campoId: 2, campo: "Quifa")
        };
        var db = await CreateContextWithWells(wells);
        var sut = new GetWellsListQueryHandler(db, _currentUser);

        // Act
        var result = await sut.Handle(new GetWellsListQuery(1, 20, CampoId: 1), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(2);
        result.Value.Items.Should().AllSatisfy(item => item.Campo.Should().Be("Rubiales"));
    }

    [Fact]
    public async Task Handle_WithDenominacionFilter_MatchesPartialString()
    {
        // Arrange — denominacion "RUBIALES" y "QUIFA" — filtramos por "RUBI" (parcial, same-case)
        // Nota: InMemory EF Core es case-sensitive; la case-insensitivity real es collation SQL Server.
        var wells = new List<Well>
        {
            BuildWell(denominacion: "RUBIALES", campoId: 1, campo: "Rubiales"),
            BuildWell(denominacion: "QUIFA01", campoId: 1, campo: "Rubiales")
        };
        var db = await CreateContextWithWells(wells);
        var sut = new GetWellsListQueryHandler(db, _currentUser);

        // Act
        var result = await sut.Handle(new GetWellsListQuery(1, 20, Denominacion: "RUBI"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(1);
        result.Value.Items.Single().NombrePozo.Should().Contain("RUBIALES");
    }
}
