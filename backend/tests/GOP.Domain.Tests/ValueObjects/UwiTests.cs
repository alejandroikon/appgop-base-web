using GOP.Domain.Enums;
using GOP.Domain.ValueObjects;
using FluentAssertions;

namespace GOP.Domain.Tests.ValueObjects;

/// <summary>
/// Tests del algoritmo UWI Fiscalizado PPDM.
/// Referencia: docs/features/creacion-pozo/uwi-algorithm.md
/// Cobertura objetivo: 100% del Value Object Uwi
/// </summary>
public sealed class UwiTests
{
    // ─── Casos del Instructivo ANH ─────────────────────────────────────────────

    [Fact]
    public void Generate_Ejemplo1_PozoOriginalVertical_DevuelveUwiCorrecto()
    {
        // Meta (50), Puerto Gaitán (50568), Cusiana Renata, 1, Locación A (abrev "LA"), V, Original, PH, OH
        var result = Uwi.Generate("50", "568", "Cusiana Renata", 1,
            "LA", 0, TipoAngulo.V, TipoTrayectoria.O, 1, TipoObjetivo.PH, TipoTerminacion.OH, false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("50568CURE0001LA0000VOPH-OH");
    }

    [Fact]
    public void Generate_Ejemplo2_SideTrackHorizontal_DevuelveUwiCorrecto()
    {
        // Santander (68), Barrancabermeja (68081), Alpha, 42, Cluster Norte 3 (abrev "CN"), H, ST2, I, CD
        var result = Uwi.Generate("68", "081", "Alpha", 42,
            "CN", 3, TipoAngulo.H, TipoTrayectoria.ST, 2, TipoObjetivo.I, TipoTerminacion.CD, false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("68081ALPH0042CN0003HST2I-CD");
    }

    [Fact]
    public void Generate_Ejemplo3_AnhEstratigraficoPutumayo_DevuelveUwiCorrecto()
    {
        // Putumayo (86), Orito (86320), Exploración Sur, 1, sin cluster, V, Original, GT, O
        var result = Uwi.Generate("86", "320", "Exploración Sur", 1,
            null, 0, TipoAngulo.V, TipoTrayectoria.O, 1, TipoObjetivo.GT, TipoTerminacion.O, true);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("86320ANHE0001CX0000VOGT-O");
    }

    // ─── Algoritmo de Sigla ────────────────────────────────────────────────────

    [Theory]
    [InlineData("Cusiana Renata", false, "CURE")]
    [InlineData("Alpha", false, "ALPH")]
    [InlineData("Sol", false, "SOLX")]
    [InlineData("Rio Magdalena Sur", false, "RIMA")]
    [InlineData("AB", false, "ABXX")]
    [InlineData("A", false, "AXXX")]
    [InlineData("Cusiana", true, "ANHC")]
    [InlineData("Exploración Sur", true, "ANHE")]
    [InlineData("La Cira", false, "LACI")] // "La" es stopword, se usa primera letra de "La" (no ignorada aquí porque es primer token)
    public void GenerateSigla_CasosDiversos_DevuelveSiglaCorrecta(
        string denominacion, bool isAnh, string siglaEsperada)
    {
        var sigla = Uwi.GenerateSigla(denominacion, isAnh);
        sigla.Should().Be(siglaEsperada);
    }

    [Fact]
    public void GenerateSigla_UnaSolaPalabraCorta_PaddingConX()
    {
        Uwi.GenerateSigla("Sol", false).Should().Be("SOLX");
        Uwi.GenerateSigla("AB", false).Should().Be("ABXX");
        Uwi.GenerateSigla("A", false).Should().Be("AXXX");
    }

    [Fact]
    public void GenerateSigla_AnhException_Prefijo_ANH_MasPrimeraLetra()
    {
        Uwi.GenerateSigla("Cusiana", true).Should().Be("ANHC");
        Uwi.GenerateSigla("Exploración", true).Should().Be("ANHE");
        Uwi.GenerateSigla("Rio", true).Should().Be("ANHR");
    }

    // ─── Algoritmo de Cluster ─────────────────────────────────────────────────

    [Theory]
    [InlineData("LA", 0, "LA0000")]   // Locación A → abreviatura "LA"
    [InlineData("CN", 3, "CN0003")]   // Cluster Norte 3 → abreviatura "CN"
    [InlineData(null, 0, "CX0000")]
    [InlineData("", 0, "CX0000")]
    [InlineData("PS", 15, "PS0015")]  // Pad Sur → abreviatura "PS"
    public void GenerateClusterCode_CasosDiversos_DevuelveCodigoCorrecto(
        string? clusterAbreviatura, int clusterNumero, string codigoEsperado)
    {
        Uwi.GenerateClusterCode(clusterAbreviatura, clusterNumero).Should().Be(codigoEsperado);
    }

    // ─── Algoritmo de Trayectoria ─────────────────────────────────────────────

    [Theory]
    [InlineData(TipoTrayectoria.O, 1, "O")]
    [InlineData(TipoTrayectoria.ST, 1, "ST")]
    [InlineData(TipoTrayectoria.ST, 2, "ST2")]
    [InlineData(TipoTrayectoria.ST, 3, "ST3")]
    [InlineData(TipoTrayectoria.G, 1, "G")]
    [InlineData(TipoTrayectoria.G, 2, "G2")]
    [InlineData(TipoTrayectoria.P, 1, "P")]
    [InlineData(TipoTrayectoria.PR, 1, "PR")]
    [InlineData(TipoTrayectoria.ML, 1, "ML")]
    public void GenerateTrayectoriaCode_CasosDiversos_DevuelveCodigoCorrecto(
        TipoTrayectoria trayectoria, int consecutivo, string codigoEsperado)
    {
        Uwi.GenerateTrayectoriaCode(trayectoria, consecutivo).Should().Be(codigoEsperado);
    }

    // ─── Números y padding ────────────────────────────────────────────────────

    [Theory]
    [InlineData(1, "0001")]
    [InlineData(42, "0042")]
    [InlineData(157, "0157")]
    [InlineData(9999, "9999")]
    public void Generate_Consecutivo_PaddingCorrecto(int consecutivo, string numeroPart)
    {
        var result = Uwi.Generate("50", "568", "Test Pozo", consecutivo,
            null, 0, TipoAngulo.V, TipoTrayectoria.O, 1, TipoObjetivo.PH, TipoTerminacion.OH, false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Numero.Should().Be(numeroPart);
    }

    // ─── Validaciones de formato ──────────────────────────────────────────────

    [Fact]
    public void Generate_DptoCodigo_DebeSerDosDigitos()
    {
        var result = Uwi.Generate("5", "568", "Test", 1,
            null, 0, TipoAngulo.V, TipoTrayectoria.O, 1, TipoObjetivo.PH, TipoTerminacion.OH, false);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Uwi.InvalidDpto");
    }

    [Fact]
    public void Generate_MpioCodigo_DebeSer3Digitos()
    {
        var result = Uwi.Generate("50", "68", "Test", 1,
            null, 0, TipoAngulo.V, TipoTrayectoria.O, 1, TipoObjetivo.PH, TipoTerminacion.OH, false);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Uwi.InvalidMpio");
    }

    [Fact]
    public void Generate_ConsecutivoFueraDeRango_Falla()
    {
        var result1 = Uwi.Generate("50", "568", "Test", 0,
            null, 0, TipoAngulo.V, TipoTrayectoria.O, 1, TipoObjetivo.PH, TipoTerminacion.OH, false);
        var result2 = Uwi.Generate("50", "568", "Test", 10000,
            null, 0, TipoAngulo.V, TipoTrayectoria.O, 1, TipoObjetivo.PH, TipoTerminacion.OH, false);

        result1.IsFailure.Should().BeTrue();
        result2.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Generate_UwiResultante_LongitudMaxima50()
    {
        // Caso más largo posible: ST3 + GT = 3 chars objetivo + trayectoria larga
        var result = Uwi.Generate("50", "568", "Cusiana Renata", 9999,
            "Locación A", 9999, TipoAngulo.D, TipoTrayectoria.ST, 3, TipoObjetivo.GT, TipoTerminacion.CC, false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Length.Should().BeLessOrEqualTo(50);
    }

    [Fact]
    public void Generate_ComponentesDesglosados_SonCorrectos()
    {
        var result = Uwi.Generate("50", "568", "Cusiana Renata", 1,
            "LA", 0, TipoAngulo.V, TipoTrayectoria.O, 1, TipoObjetivo.PH, TipoTerminacion.OH, false);

        result.IsSuccess.Should().BeTrue();
        var uwi = result.Value;
        uwi.DptoCode.Should().Be("50");
        uwi.MpioCode.Should().Be("568");
        uwi.Sigla.Should().Be("CURE");
        uwi.Numero.Should().Be("0001");
        uwi.ClusterCode.Should().Be("LA0000");
        uwi.AnguloCode.Should().Be("V");
        uwi.TrayectoriaCode.Should().Be("O");
        uwi.ObjetivoCode.Should().Be("PH");
        uwi.TerminacionCode.Should().Be("OH");
    }

    [Fact]
    public void Generate_Igualdad_MismoValorIgualUwi()
    {
        var r1 = Uwi.Generate("50", "568", "Cusiana Renata", 1,
            "Locación A", 0, TipoAngulo.V, TipoTrayectoria.O, 1, TipoObjetivo.PH, TipoTerminacion.OH, false);
        var r2 = Uwi.Generate("50", "568", "Cusiana Renata", 1,
            "Locación A", 0, TipoAngulo.V, TipoTrayectoria.O, 1, TipoObjetivo.PH, TipoTerminacion.OH, false);

        r1.Value.Should().Be(r2.Value);
    }

    [Fact]
    public void Generate_ToStringDevuelveValue()
    {
        var result = Uwi.Generate("50", "568", "Alpha", 1,
            null, 0, TipoAngulo.V, TipoTrayectoria.O, 1, TipoObjetivo.PH, TipoTerminacion.OH, false);

        result.Value.ToString().Should().Be(result.Value.Value);
    }
}
