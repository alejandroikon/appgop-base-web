using FluentAssertions;
using GOP.Application.Features.Wells.Queries.PreviewUwi;
using GOP.Domain.Interfaces.Repositories;
using NSubstitute;

namespace GOP.Application.Tests.Features.Wells.Queries;

public sealed class PreviewUwiQueryHandlerTests
{
    private readonly IWellRepository _wellRepo = Substitute.For<IWellRepository>();

    [Fact]
    public async Task Handle_ParametrosValidos_DevuelveUwiCorrecto()
    {
        _wellRepo.ExistsByUwiAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = new PreviewUwiQueryHandler(_wellRepo);
        var query = new PreviewUwiQuery(
            CodigoDaneDpto: "50",
            CodigoDaneMpio: "568",
            Denominacion: "Cusiana Renata",
            Consecutivo: 1,
            ClusterNombre: "LA",     // abreviatura del cluster, no el nombre completo
            ClusterNumero: 0,
            TipoAngulo: "V",
            TipoTrayectoria: "O",
            TrayectoriaConsecutivo: 1,
            TipoObjetivo: "PH",
            TipoTerminacion: "OH",
            IsAnh: false,
            ExcludeWellId: null);

        var result = await sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Uwi.Should().Be("50568CURE0001LA0000VOPH-OH");
        result.Value.IsUnique.Should().BeTrue();
        result.Value.Components.Sigla.Should().Be("CURE");
    }

    [Fact]
    public async Task Handle_UwiExistente_IsUniqueEsFalse()
    {
        _wellRepo.ExistsByUwiAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = new PreviewUwiQueryHandler(_wellRepo);
        var query = new PreviewUwiQuery("50", "568", "Cusiana Renata", 1,
            null, 0, "V", "O", 1, "PH", "OH", false, null);

        var result = await sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsUnique.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_TipoAnguloInvalido_RetornaFailure()
    {
        var sut = new PreviewUwiQueryHandler(_wellRepo);
        var query = new PreviewUwiQuery("50", "568", "Test", 1,
            null, 0, "X", "O", 1, "PH", "OH", false, null);

        var result = await sut.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PreviewUwi.InvalidAngulo");
    }

    [Fact]
    public async Task Handle_AnhEstratigrafico_SiglaConPrefixoANH()
    {
        _wellRepo.ExistsByUwiAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = new PreviewUwiQueryHandler(_wellRepo);
        var query = new PreviewUwiQuery("86", "320", "Exploración Sur", 1,
            null, 0, "V", "O", 1, "GT", "O", true, null);

        var result = await sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Components.Sigla.Should().Be("ANHE");
        result.Value.Uwi.Should().StartWith("86320ANHE");
    }
}
