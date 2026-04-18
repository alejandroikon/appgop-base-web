using FluentAssertions;
using GOP.Application.Features.Wells.Commands.CreateCluster;
using GOP.Application.Tests.Common;
using GOP.Domain.Entities;
using GOP.Domain.Interfaces;
using NSubstitute;

namespace GOP.Application.Tests.Features.Wells.Commands;

public sealed class CreateClusterCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private async Task<TestDbContext> CreateDbWithCampo()
    {
        var db = TestDbContext.Create();
        db.Campos.Add(new Campo { Id = 1, Nombre = "Rubiales", ContratoId = 1 });
        await db.SaveChangesAsync();

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ci => db.SaveChangesAsync(ci.Arg<CancellationToken>()));

        return db;
    }

    [Fact]
    public async Task Handle_NombreNuevo_CreaClusterConAbreviatura()
    {
        var db = await CreateDbWithCampo();
        var sut = new CreateClusterCommandHandler(db, _unitOfWork);

        var result = await sut.Handle(new CreateClusterCommand("Locación B", 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Locación B");
        // "Locación B" → primera letra de "Locación" = L, primera letra de "B" = B → "LB"
        result.Value.Abreviatura.Should().Be("LB");
        result.Value.CampoId.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NombreDuplicado_Retorna409()
    {
        var db = await CreateDbWithCampo();
        // Agregar cluster existente
        db.Clusters.Add(new Cluster { Id = 99, Nombre = "Locación B", Abreviatura = "LO", CampoId = 1 });
        await db.SaveChangesAsync();
        var sut = new CreateClusterCommandHandler(db, _unitOfWork);

        var result = await sut.Handle(new CreateClusterCommand("Locación B", 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Cluster.DuplicateInCampo");
    }

    [Fact]
    public async Task Handle_CampoNoExiste_RetornaError()
    {
        var db = TestDbContext.Create();
        var sut = new CreateClusterCommandHandler(db, _unitOfWork);

        var result = await sut.Handle(new CreateClusterCommand("Locación X", 999), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
