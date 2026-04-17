using FluentAssertions;
using GOP.Application.Features.Wells.Queries.PreviewWellName;
using GOP.Application.Tests.Common;
using GOP.Domain.Entities;
using GOP.Domain.Enums;
using GOP.Domain.Errors;

namespace GOP.Application.Tests.Features.Wells.Queries;

public sealed class PreviewWellNameQueryHandlerTests
{
    private static PreviewWellNameQuery ValidQuery(Guid? excludeWellId = null) => new(
        ContratoId: 1,
        Denominacion: "ALPHA",
        Consecutivo: "01",
        ExcludeWellId: excludeWellId);

    private static async Task<TestDbContext> CreateContextWithContrato()
    {
        var db = TestDbContext.Create();
        db.Contratos.Add(new Contrato
        {
            Id = 1,
            Nombre = "Contrato E&P Llanos",
            Tipo = "E&P",
            Cuenca = "Llanos Orientales"
        });
        await db.SaveChangesAsync();
        return db;
    }

    private static Well CreateWell(string cuenca, string denominacion, string consecutivo)
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
            cuenca: cuenca,
            campoId: 1,
            tipoTrayectoria: TipoTrayectoria.ST,
            clasificacion: Clasificacion.Exploratorio,
            denominacion: denominacion,
            consecutivo: consecutivo,
            tipoUbicacion: TipoUbicacion.Continental,
            tipoAngulo: TipoAngulo.V,
            tipoObjetivo: TipoObjetivo.PH,
            tipoTerminacion: TipoTerminacion.CD,
            location: location);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsAvailableName()
    {
        // Arrange
        var db = await CreateContextWithContrato();
        var sut = new PreviewWellNameQueryHandler(db);

        // Act
        var result = await sut.Handle(ValidQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NombrePozo.Should().Be("Llanos Orientales-ALPHA-01");
        result.Value.Available.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DuplicateName_ReturnsUnavailable()
    {
        // Arrange
        var db = await CreateContextWithContrato();
        db.Wells.Add(CreateWell("Llanos Orientales", "ALPHA", "01"));
        await db.SaveChangesAsync();

        var sut = new PreviewWellNameQueryHandler(db);

        // Act
        var result = await sut.Handle(ValidQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NombrePozo.Should().Be("Llanos Orientales-ALPHA-01");
        result.Value.Available.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ContratoNotFound_ReturnsFailure()
    {
        // Arrange
        var db = TestDbContext.Create(); // Sin contratos
        var sut = new PreviewWellNameQueryHandler(db);
        var query = ValidQuery() with { ContratoId = 999 };

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contrato.NotFound);
    }

    [Fact]
    public async Task Handle_ExcludeWellId_ExcludesFromCheck()
    {
        // Arrange
        var db = await CreateContextWithContrato();
        var well = CreateWell("Llanos Orientales", "ALPHA", "01");
        db.Wells.Add(well);
        await db.SaveChangesAsync();

        var sut = new PreviewWellNameQueryHandler(db);
        var query = ValidQuery(excludeWellId: well.Id);

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NombrePozo.Should().Be("Llanos Orientales-ALPHA-01");
        result.Value.Available.Should().BeTrue(); // El pozo existente fue excluido
    }
}
