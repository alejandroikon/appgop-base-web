using FluentAssertions;
using GOP.Application.Features.Wells.Queries.PreviewWellName;
using GOP.Application.Tests.Common;
using GOP.Domain.Entities;
using GOP.Domain.Enums;
using GOP.Domain.Errors;
using GOP.Domain.Interfaces.Repositories;
using NSubstitute;

namespace GOP.Application.Tests.Features.Wells.Queries;

public sealed class PreviewWellNameQueryHandlerTests
{
    private readonly IWellRepository _wellRepo = Substitute.For<IWellRepository>();

    private static PreviewWellNameQuery ValidQuery(Guid? excludeWellId = null) => new(
        ContratoId: 1,
        CampoId: null,
        Denominacion: "ALPHA",
        Consecutivo: 1,
        ExcludeWellId: excludeWellId);

    private static async Task<TestDbContext> CreateContextWithContrato()
    {
        var db = TestDbContext.Create();
        db.Contratos.Add(new Contrato
        {
            Id = 1,
            Nombre = "E&P Llanos",
            Tipo = "E&P",
            Cuenca = "Llanos Orientales"
        });
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task Handle_ValidQuery_RetornaIsUniqueTrue()
    {
        var db = await CreateContextWithContrato();
        var sut = new PreviewWellNameQueryHandler(db, _wellRepo);

        var result = await sut.Handle(ValidQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NombrePozo.Should().Contain("ALPHA");
        result.Value.IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NombreDuplicado_RetornaIsUniqueFalse()
    {
        var db = await CreateContextWithContrato();
        // Agregar un pozo con el mismo nombre
        var nombrePozo = "LLANOS ORIENTALES-ALPHA-1";
        db.Wells.Add(Well.CreateDraft(
            operadora: "Ecopetrol", tenantId: 1, contratoId: 1, contrato: "E&P Llanos",
            tipoContrato: "E&P", cuenca: "Llanos Orientales", campoId: null, campo: null,
            denominacion: "ALPHA", consecutivo: 1, tipoTrayectoria: TipoTrayectoria.O,
            clasificacion: Clasificacion.Exploratorio, subClasificacion: null,
            tipoUbicacion: TipoUbicacion.Continental, tipoAngulo: TipoAngulo.V,
            tipoObjetivo: TipoObjetivo.PH, tipoTerminacion: TipoTerminacion.OH,
            departamentoId: null, departamento: null, codigoDaneDpto: null,
            municipioId: null, municipio: null, codigoDaneMpio: null,
            clusterId: null, cluster: null));
        await db.SaveChangesAsync();
        var sut = new PreviewWellNameQueryHandler(db, _wellRepo);

        var result = await sut.Handle(ValidQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsUnique.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ContratoNotFound_RetornaFailure()
    {
        var db = TestDbContext.Create();
        var sut = new PreviewWellNameQueryHandler(db, _wellRepo);

        var result = await sut.Handle(ValidQuery() with { ContratoId = 999 }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contrato.NotFound);
    }
}
