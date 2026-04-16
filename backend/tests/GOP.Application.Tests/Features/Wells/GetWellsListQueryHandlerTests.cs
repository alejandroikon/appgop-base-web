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

    private static Well BuildWell(int tenantId = 1, int contratoId = 1, int campoId = 1,
        string denominacion = "ALPHA")
        => Well.Create("Ecopetrol", tenantId, contratoId, "E&P", "Llanos",
            campoId, TipoTrayectoria.ST, Clasificacion.Exploratorio, denominacion, "01",
            TipoUbicacion.Continental, TipoAngulo.V, TipoObjetivo.PH, TipoTerminacion.CD,
            new WellLocation
            {
                DepartamentoId = 1, MunicipioId = 1,
                CodigoDaneDpto = "50", CodigoDaneMpio = "50568"
            });

    private async Task<TestDbContext> CreateContextWithWells(List<Well> wells)
    {
        var db = TestDbContext.Create();
        db.Contratos.Add(new Contrato { Id = 1, Nombre = "Contrato Llanos", Tipo = "E&P", Cuenca = "Llanos" });
        db.Contratos.Add(new Contrato { Id = 2, Nombre = "Contrato Piedemonte", Tipo = "E&P", Cuenca = "Piedemonte" });
        db.Campos.Add(new Campo { Id = 1, Nombre = "Campo Rubiales", ContratoId = 1 });
        db.Campos.Add(new Campo { Id = 2, Nombre = "Campo Cusiana", ContratoId = 2 });
        db.Wells.AddRange(wells);
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task Handle_ReturnsPagedList()
    {
        // Arrange
        var wells = new List<Well> { BuildWell(), BuildWell(denominacion: "BETA") };
        var db = await CreateContextWithWells(wells);
        var sut = new GetWellsListQueryHandler(db, _currentUser);

        // Act
        var result = await sut.Handle(new GetWellsListQuery(Page: 1, PageSize: 20), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_FilterByContratoId_ReturnsFiltered()
    {
        // Arrange
        var wells = new List<Well>
        {
            BuildWell(contratoId: 1),
            BuildWell(contratoId: 2, denominacion: "GAMMA")
        };
        var db = await CreateContextWithWells(wells);
        var sut = new GetWellsListQueryHandler(db, _currentUser);

        // Act
        var result = await sut.Handle(new GetWellsListQuery(Page: 1, PageSize: 20, ContratoId: 1), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(1);
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_SearchByNombrePozo_ReturnsMatching()
    {
        // Arrange
        var wells = new List<Well>
        {
            BuildWell(denominacion: "ALPHA"),
            BuildWell(denominacion: "BETA")
        };
        var db = await CreateContextWithWells(wells);
        var sut = new GetWellsListQueryHandler(db, _currentUser);

        // Act
        var result = await sut.Handle(new GetWellsListQuery(Page: 1, PageSize: 20, Search: "ALPHA"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(1);
        result.Value.Items[0].NombrePozo.Should().Contain("ALPHA");
    }
}
