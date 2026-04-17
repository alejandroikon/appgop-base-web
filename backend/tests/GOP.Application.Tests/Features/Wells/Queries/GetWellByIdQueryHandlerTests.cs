using FluentAssertions;
using GOP.Application.Features.Wells.Queries.GetWellById;
using GOP.Application.Tests.Common;
using GOP.Domain.Entities;
using GOP.Domain.Enums;
using GOP.Domain.Interfaces.Services;
using NSubstitute;

namespace GOP.Application.Tests.Features.Wells.Queries;

public sealed class GetWellByIdQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    public GetWellByIdQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(1);
        _currentUser.IsInRole("ADMIN").Returns(false);
        _currentUser.IsInRole("AUDITOR").Returns(false);
    }

    private static Well BuildWell(int tenantId = 1) => Well.Create(
        operadora: "Ecopetrol S.A.",
        tenantId: tenantId,
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

    private static async Task<TestDbContext> CreateContextWithWell(Well well)
    {
        var db = TestDbContext.Create();
        db.Contratos.Add(new Contrato { Id = 1, Nombre = "Contrato E&P Llanos", Tipo = "E&P", Cuenca = "Llanos Orientales" });
        db.Campos.Add(new Campo { Id = 1, Nombre = "Campo Rubiales", ContratoId = 1 });
        db.Departamentos.Add(new Departamento { Id = 1, Nombre = "Meta", CodigoDane = "50" });
        db.Municipios.Add(new Municipio { Id = 1, Nombre = "Puerto Gaitán", DepartamentoId = 1, CodigoDane = "50568" });
        db.Wells.Add(well);
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task Handle_WellExistente_ReturnWellDetailDto()
    {
        // Arrange
        var well = BuildWell();
        var db = await CreateContextWithWell(well);
        var sut = new GetWellByIdQueryHandler(db, _currentUser);

        // Act
        var result = await sut.Handle(new GetWellByIdQuery(well.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(well.Id);
        result.Value.NombrePozo.Should().Be("Llanos Orientales-ALPHA-01");
        result.Value.Estado.Should().Be("Borrador");
        result.Value.Contrato.Should().Be("Contrato E&P Llanos");
        result.Value.Campo.Should().Be("Campo Rubiales");
    }

    [Fact]
    public async Task Handle_WellNotFound_ReturnsFailure()
    {
        // Arrange
        var db = TestDbContext.Create();
        var sut = new GetWellByIdQueryHandler(db, _currentUser);

        // Act
        var result = await sut.Handle(new GetWellByIdQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Well.NotFound");
    }

    [Fact]
    public async Task Handle_AdminRole_ReturnsWellDetailSinFiltroTenant()
    {
        // Arrange: admin puede ver pozos de cualquier tenant
        _currentUser.IsInRole("ADMIN").Returns(true);
        var well = BuildWell(tenantId: 99); // tenant diferente al currentUser.TenantId (1)
        var db = await CreateContextWithWell(well);
        var sut = new GetWellByIdQueryHandler(db, _currentUser);

        // Act
        var result = await sut.Handle(new GetWellByIdQuery(well.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(well.Id);
    }
}
