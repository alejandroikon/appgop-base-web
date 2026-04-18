using FluentAssertions;
using GOP.Domain.Entities;
using GOP.Domain.Enums;

namespace GOP.Domain.Tests.Entities;

public sealed class WellTests
{
    private static Well BuildDraft(
        string denominacion = "Cusiana Renata",
        int consecutivo = 1,
        Clasificacion clasificacion = Clasificacion.Exploratorio)
    {
        return Well.CreateDraft(
            operadora: "Ecopetrol",
            tenantId: 1,
            contratoId: 1,
            contrato: "E&P Llanos",
            tipoContrato: "E&P",
            cuenca: "Llanos Orientales",
            campoId: 1,
            campo: "Rubiales",
            denominacion: denominacion,
            consecutivo: consecutivo,
            tipoTrayectoria: TipoTrayectoria.O,
            clasificacion: clasificacion,
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
    }

    // ─── CreateDraft ──────────────────────────────────────────────────────────

    [Fact]
    public void CreateDraft_DatosMinimos_EstadoEsBorrador()
    {
        var well = BuildDraft();

        well.Estado.Should().Be(WellStatus.Borrador);
        well.Uwi.Should().BeNull();
        well.Forma101Radicada.Should().BeFalse();
    }

    [Fact]
    public void CreateDraft_DenominacionEnMayusculas()
    {
        var well = BuildDraft(denominacion: "cusiana renata");
        well.Denominacion.Should().Be("CUSIANA RENATA");
    }

    [Fact]
    public void CreateDraft_NombrePozoGeneradoCorrectamente()
    {
        var well = BuildDraft(denominacion: "Cusiana Renata", consecutivo: 1);
        well.NombrePozo.Should().Be("RUBIALES-CUSIANA RENATA-1");
    }

    // ─── CreateFinalized ──────────────────────────────────────────────────────

    [Fact]
    public void CreateFinalized_DatosCompletos_EstadoEsCreado()
    {
        var well = Well.CreateFinalized(
            operadora: "Ecopetrol",
            tenantId: 1,
            contratoId: 1,
            contrato: "E&P Llanos",
            tipoContrato: "E&P",
            cuenca: "Llanos Orientales",
            campoId: 1,
            campo: "Rubiales",
            denominacion: "Cusiana Renata",
            consecutivo: 1,
            tipoTrayectoria: TipoTrayectoria.O,
            clasificacion: Clasificacion.Exploratorio,
            subClasificacion: SubClasificacionExploratoria.A3,
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
            cluster: null,
            uwi: "50568CURE0001CX0000VPH-OH");

        well.Estado.Should().Be(WellStatus.Creado);
        well.Uwi.Should().Be("50568CURE0001CX0000VPH-OH");
    }

    // ─── IsEditable / IsDeletable ─────────────────────────────────────────────

    [Fact]
    public void IsEditable_Borrador_EsEditable()
    {
        BuildDraft().IsEditable().Should().BeTrue();
    }

    [Fact]
    public void IsEditable_Forma101Radicada_NoEsEditable()
    {
        // Un pozo CREADO con Forma101Radicada no es editable
        var well = Well.CreateFinalized(
            operadora: "Ecopetrol", tenantId: 1, contratoId: 1, contrato: "E&P",
            tipoContrato: "E&P", cuenca: "Llanos", campoId: null, campo: null,
            denominacion: "Test", consecutivo: 1, tipoTrayectoria: TipoTrayectoria.O,
            clasificacion: Clasificacion.Exploratorio, subClasificacion: null,
            tipoUbicacion: TipoUbicacion.Continental, tipoAngulo: TipoAngulo.V,
            tipoObjetivo: TipoObjetivo.PH, tipoTerminacion: TipoTerminacion.OH,
            departamentoId: 1, departamento: "Meta", codigoDaneDpto: "50",
            municipioId: 1, municipio: "PG", codigoDaneMpio: "568",
            clusterId: null, cluster: null, uwi: "50568TEST0001CX0000VPH-OH");
        well.MarkForma101Radicada();

        well.IsEditable().Should().BeFalse();
    }

    [Fact]
    public void IsDeletable_Borrador_EsDeletable()
    {
        BuildDraft().IsDeletable().Should().BeTrue();
    }

    [Fact]
    public void IsDeletable_Forma101Radicada_NoEsDeletable()
    {
        // Un pozo CREADO con Forma101Radicada no es eliminable
        var well = Well.CreateFinalized(
            operadora: "Ecopetrol", tenantId: 1, contratoId: 1, contrato: "E&P",
            tipoContrato: "E&P", cuenca: "Llanos", campoId: null, campo: null,
            denominacion: "Test", consecutivo: 1, tipoTrayectoria: TipoTrayectoria.O,
            clasificacion: Clasificacion.Exploratorio, subClasificacion: null,
            tipoUbicacion: TipoUbicacion.Continental, tipoAngulo: TipoAngulo.V,
            tipoObjetivo: TipoObjetivo.PH, tipoTerminacion: TipoTerminacion.OH,
            departamentoId: 1, departamento: "Meta", codigoDaneDpto: "50",
            municipioId: 1, municipio: "PG", codigoDaneMpio: "568",
            clusterId: null, cluster: null, uwi: "50568TEST0001CX0000VPH-OH");
        well.MarkForma101Radicada();

        well.IsDeletable().Should().BeFalse();
    }

    // ─── Finalize ─────────────────────────────────────────────────────────────

    [Fact]
    public void Finalize_DesdeBorrador_CambiaACreado()
    {
        var well = BuildDraft();
        var result = well.Finalize("50568CURE0001CX0000VPH-OH");

        result.IsSuccess.Should().BeTrue();
        well.Estado.Should().Be(WellStatus.Creado);
        well.Uwi.Should().Be("50568CURE0001CX0000VPH-OH");
    }

    [Fact]
    public void Finalize_YaFinalizado_Falla()
    {
        var well = BuildDraft();
        well.Finalize("50568CURE0001CX0000VPH-OH");

        var result2 = well.Finalize("50568CURE0001CX0000VPH-OH");
        result2.IsFailure.Should().BeTrue();
    }

    // ─── SoftDelete ───────────────────────────────────────────────────────────

    [Fact]
    public void SoftDelete_Borrador_MarcaComoEliminado()
    {
        var well = BuildDraft();
        var result = well.SoftDelete();

        result.IsSuccess.Should().BeTrue();
        well.IsDeleted.Should().BeTrue();
        well.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void SoftDelete_Forma101_Falla()
    {
        // Un pozo CREADO con Forma101Radicada no puede eliminarse
        var well = Well.CreateFinalized(
            operadora: "Ecopetrol", tenantId: 1, contratoId: 1, contrato: "E&P",
            tipoContrato: "E&P", cuenca: "Llanos", campoId: null, campo: null,
            denominacion: "Test", consecutivo: 1, tipoTrayectoria: TipoTrayectoria.O,
            clasificacion: Clasificacion.Exploratorio, subClasificacion: null,
            tipoUbicacion: TipoUbicacion.Continental, tipoAngulo: TipoAngulo.V,
            tipoObjetivo: TipoObjetivo.PH, tipoTerminacion: TipoTerminacion.OH,
            departamentoId: 1, departamento: "Meta", codigoDaneDpto: "50",
            municipioId: 1, municipio: "PG", codigoDaneMpio: "568",
            clusterId: null, cluster: null, uwi: "50568TEST0001CX0000VPH-OH");
        well.MarkForma101Radicada();

        var result = well.SoftDelete();

        result.IsFailure.Should().BeTrue();
        well.IsDeleted.Should().BeFalse();
    }
}
