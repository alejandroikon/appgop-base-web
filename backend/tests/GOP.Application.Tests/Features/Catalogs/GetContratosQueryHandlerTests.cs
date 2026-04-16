using FluentAssertions;
using GOP.Application.Features.Catalogs.Queries.GetContratos;
using GOP.Application.Tests.Common;
using GOP.Domain.Entities;

namespace GOP.Application.Tests.Features.Catalogs;

public sealed class GetContratosQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllContratos()
    {
        // Arrange
        var db = TestDbContext.Create();
        db.Contratos.AddRange(
            new Contrato { Id = 1, Nombre = "Contrato E&P Llanos", Tipo = "E&P", Cuenca = "Llanos Orientales" },
            new Contrato { Id = 2, Nombre = "Contrato E&P Piedemonte", Tipo = "E&P", Cuenca = "Piedemonte" }
        );
        await db.SaveChangesAsync();
        var sut = new GetContratosQueryHandler(db);

        // Act
        var result = await sut.Handle(new GetContratosQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ReturnsCorrectDtoShape()
    {
        // Arrange
        var db = TestDbContext.Create();
        db.Contratos.Add(new Contrato { Id = 1, Nombre = "Contrato E&P Llanos", Tipo = "E&P", Cuenca = "Llanos Orientales" });
        await db.SaveChangesAsync();
        var sut = new GetContratosQueryHandler(db);

        // Act
        var result = await sut.Handle(new GetContratosQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Single();
        dto.Id.Should().Be(1);
        dto.Nombre.Should().Be("Contrato E&P Llanos");
        dto.Tipo.Should().Be("E&P");
        dto.Cuenca.Should().Be("Llanos Orientales");
    }
}
