using FluentAssertions;
using GOP.Domain.ValueObjects;

namespace GOP.Domain.Tests.ValueObjects;

public sealed class UwiTests
{
    [Fact]
    public void Create_ValidData_ReturnsFormattedUwi()
    {
        // Arrange
        var daneDpto = "50";
        var daneMpio = "50568";
        var denominacion = "ALPHA";
        var consecutivo = "01";
        var trayectoria = "ST";

        // Act
        var result = Uwi.Create(daneDpto, daneMpio, denominacion, consecutivo, trayectoria);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("CO-50-50568-ALPHA-01-ST");
    }

    [Fact]
    public void Create_DenominacionWithAccents_NormalizesToAscii()
    {
        // Arrange — denominación con diacríticos: NIÑO → NINO, ÁNGEL → ANGEL
        var result = Uwi.Create("50", "50568", "niño", "02", "ST");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("CO-50-50568-NINO-02-ST");
    }

    [Fact]
    public void Create_ResultExceeds50Chars_ReturnsFailure()
    {
        // Arrange — denominación muy larga que supera el límite de 50 caracteres en total
        var denominacionLarga = "DENOMINACIONMUYLARGANOMBREEXTREMADAMENTELARGO";

        // Act
        var result = Uwi.Create("50", "50568", denominacionLarga, "01", "ST");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Uwi.TooLong");
    }

    [Fact]
    public void Create_LowercaseDenominacion_ConvertsToUppercase()
    {
        // Arrange
        var result = Uwi.Create("05", "05001", "alpha", "01", "P");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("CO-05-05001-ALPHA-01-P");
    }
}
