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

    private static Well BuildWell(int tenantId = 1) => Well.CreateDraft(
        operadora: "Ecopetrol S.A.",
        tenantId: tenantId,
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

    private static async Task<TestDbContext> CreateContextWithWell(Well well)
    {
        var db = TestDbContext.Create();
        db.Wells.Add(well);
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task Handle_WellExistente_RetornaWellDetailDto()
    {
        var well = BuildWell();
        var db = await CreateContextWithWell(well);
        var sut = new GetWellByIdQueryHandler(db, _currentUser);

        var result = await sut.Handle(new GetWellByIdQuery(well.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(well.Id);
        result.Value.Operadora.Should().Be("Ecopetrol S.A.");
    }

    [Fact]
    public async Task Handle_WellNoEncontrado_RetornaFailure()
    {
        var db = TestDbContext.Create();
        var sut = new GetWellByIdQueryHandler(db, _currentUser);

        var result = await sut.Handle(new GetWellByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Well.NotFound");
    }

    [Fact]
    public async Task Handle_Admin_VeWellsDeCualquierTenant()
    {
        _currentUser.IsInRole("ADMIN").Returns(true);
        var well = BuildWell(tenantId: 99); // diferente tenant
        var db = await CreateContextWithWell(well);
        var sut = new GetWellByIdQueryHandler(db, _currentUser);

        var result = await sut.Handle(new GetWellByIdQuery(well.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
