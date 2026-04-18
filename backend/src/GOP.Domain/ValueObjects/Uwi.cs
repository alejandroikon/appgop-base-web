using GOP.Domain.Common;
using GOP.Domain.Enums;
using System.Text;
using System.Text.RegularExpressions;

namespace GOP.Domain.ValueObjects;

/// <summary>
/// Value Object que representa el UWI Fiscalizado PPDM.
/// Estructura: [DptoDANE][MpioDANE][Sigla][Número][Cluster][Ángulo][Trayectoria][Objetivo]-[Terminación]
/// Referencia: docs/features/creacion-pozo/uwi-algorithm.md
/// </summary>
public sealed class Uwi : ValueObject
{
    public string Value { get; }

    // Componentes desglosados para auditoría y preview
    public string DptoCode { get; }        // 2 dígitos
    public string MpioCode { get; }        // 3 dígitos
    public string Sigla { get; }           // 4 chars
    public string Numero { get; }          // 4 dígitos
    public string ClusterCode { get; }     // 6 chars (2α + 4n)
    public string AnguloCode { get; }      // 1 char
    public string TrayectoriaCode { get; } // variable (vacío si Original)
    public string ObjetivoCode { get; }    // variable
    public string TerminacionCode { get; } // variable (sin guión)

    private static readonly Regex ValidCharsRegex = new(@"^[A-Z0-9\-]+$", RegexOptions.Compiled);

    private Uwi(
        string value,
        string dptoCode,
        string mpioCode,
        string sigla,
        string numero,
        string clusterCode,
        string anguloCode,
        string trayectoriaCode,
        string objetivoCode,
        string terminacionCode)
    {
        Value = value;
        DptoCode = dptoCode;
        MpioCode = mpioCode;
        Sigla = sigla;
        Numero = numero;
        ClusterCode = clusterCode;
        AnguloCode = anguloCode;
        TrayectoriaCode = trayectoriaCode;
        ObjetivoCode = objetivoCode;
        TerminacionCode = terminacionCode;
    }

    /// <summary>
    /// Genera el UWI Fiscalizado PPDM según el instructivo ANH.
    /// </summary>
    /// <param name="codigoDaneDpto">Código DANE del departamento (2 dígitos, ej. "50")</param>
    /// <param name="codigoDaneMpio">Código DANE de la parte municipal (3 dígitos, ej. "568")</param>
    /// <param name="denominacion">Denominación del pozo (ej. "Cusiana Renata")</param>
    /// <param name="consecutivo">Consecutivo 1-9999</param>
    /// <param name="clusterNombre">Nombre del cluster (nullable = pozo individual → CX0000)</param>
    /// <param name="clusterNumero">Número secuencial del cluster para padding (0 si no aplica)</param>
    /// <param name="tipoAngulo">Tipo de ángulo H/V/D</param>
    /// <param name="tipoTrayectoria">Tipo de trayectoria</param>
    /// <param name="trayectoriaConsecutivo">Consecutivo de trayectoria para ST/G (0 = primero, no se incluye número)</param>
    /// <param name="tipoObjetivo">Tipo de objetivo</param>
    /// <param name="tipoTerminacion">Tipo de terminación</param>
    /// <param name="isAnh">Si la operadora es ANH → excepción de sigla</param>
    public static Result<Uwi> Generate(
        string codigoDaneDpto,
        string codigoDaneMpio,
        string denominacion,
        int consecutivo,
        string? clusterNombre,
        int clusterNumero,
        TipoAngulo tipoAngulo,
        TipoTrayectoria tipoTrayectoria,
        int trayectoriaConsecutivo,
        TipoObjetivo tipoObjetivo,
        TipoTerminacion tipoTerminacion,
        bool isAnh)
    {
        // ─── Validaciones de entrada ───────────────────────────────────────
        if (string.IsNullOrWhiteSpace(codigoDaneDpto) || codigoDaneDpto.Length != 2)
            return Result.Failure<Uwi>(new Error("Uwi.InvalidDpto",
                "El código DANE del departamento debe tener exactamente 2 dígitos."));

        if (string.IsNullOrWhiteSpace(codigoDaneMpio) || codigoDaneMpio.Length != 3)
            return Result.Failure<Uwi>(new Error("Uwi.InvalidMpio",
                "La parte municipal del código DANE debe tener exactamente 3 dígitos."));

        if (string.IsNullOrWhiteSpace(denominacion))
            return Result.Failure<Uwi>(new Error("Uwi.InvalidDenominacion",
                "La denominación del pozo es requerida."));

        if (consecutivo < 1 || consecutivo > 9999)
            return Result.Failure<Uwi>(new Error("Uwi.InvalidConsecutivo",
                "El consecutivo debe estar entre 1 y 9999."));

        // ─── Segmento 1: DptoDANE (2 dígitos) ────────────────────────────
        var dptoCode = codigoDaneDpto.PadLeft(2, '0');

        // ─── Segmento 2: MpioDANE (3 dígitos) ────────────────────────────
        var mpioCode = codigoDaneMpio.PadLeft(3, '0');

        // ─── Segmento 3: Sigla (4 chars, fija) ───────────────────────────
        var sigla = GenerateSigla(denominacion, isAnh);

        // ─── Segmento 4: Número (4 dígitos, zero-padding) ────────────────
        var numero = consecutivo.ToString("D4");

        // ─── Segmento 5: Cluster/Locación (6 chars: 2α + 4n) ─────────────
        var clusterCode = GenerateClusterCode(clusterNombre, clusterNumero);

        // ─── Segmento 6: Ángulo (1 char) ─────────────────────────────────
        var anguloCode = tipoAngulo.ToString(); // H, V, D

        // ─── Segmento 7: Trayectoria (variable, vacío si Original) ────────
        var trayectoriaCode = GenerateTrayectoriaCode(tipoTrayectoria, trayectoriaConsecutivo);

        // ─── Segmento 8: Objetivo (variable) ─────────────────────────────
        var objetivoCode = tipoObjetivo.ToString(); // PH, I, M, D, C, GT, O

        // ─── Segmento 9: Terminación (variable, sin guión) ───────────────
        var terminacionCode = tipoTerminacion.ToString(); // CD, LC, LR, GP, CC, OH, O

        // ─── Construir el UWI completo ────────────────────────────────────
        var uwiValue = $"{dptoCode}{mpioCode}{sigla}{numero}{clusterCode}{anguloCode}{trayectoriaCode}{objetivoCode}-{terminacionCode}";

        if (uwiValue.Length > 50)
            return Result.Failure<Uwi>(new Error("Uwi.TooLong",
                $"El UWI generado '{uwiValue}' excede el máximo de 50 caracteres."));

        if (!ValidCharsRegex.IsMatch(uwiValue))
            return Result.Failure<Uwi>(new Error("Uwi.InvalidChars",
                "El UWI contiene caracteres no permitidos (solo A-Z, 0-9 y guión)."));

        return Result.Success(new Uwi(
            uwiValue, dptoCode, mpioCode, sigla, numero,
            clusterCode, anguloCode, trayectoriaCode, objetivoCode, terminacionCode));
    }

    /// <summary>
    /// Genera la sigla de 4 caracteres según el algoritmo PPDM:
    /// - ANH: "ANH" + primera letra de la denominación
    /// - 1 palabra: primeras 4 letras + padding 'X'
    /// - 2+ palabras: 2 primeras letras palabra 1 + 2 primeras letras palabra 2
    /// Ignora artículos/preposiciones: de, del, la, el, los, las, en, y, o, a
    /// </summary>
    internal static string GenerateSigla(string denominacion, bool isAnh)
    {
        // Extraer solo letras, eliminar todo lo no alfabético
        var cleanDenom = ExtractLettersOnly(denominacion).ToUpperInvariant();

        // Excepción ANH: ANH + primera letra de la denominación
        if (isAnh)
        {
            var firstLetter = cleanDenom.Length > 0 ? cleanDenom[0].ToString() : "X";
            return ("ANH" + firstLetter).PadRight(4, 'X')[..4];
        }

        // Separar en palabras significativas (ignorar artículos y preposiciones)
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "de", "del", "en", "y", "o", "un", "una" };

        var words = denominacion
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(w => ExtractLettersOnly(w).ToUpperInvariant())
            .Where(w => w.Length > 0 && (w.Length == 1 || !stopWords.Contains(w)))
            .ToArray();

        if (words.Length == 0)
            return "XXXX";

        if (words.Length == 1)
        {
            // Una sola palabra: primeras 4 letras + padding X
            return words[0].PadRight(4, 'X')[..4];
        }

        // Dos o más palabras: 2 primeras letras palabra 1 + 2 primeras letras palabra 2
        var part1 = words[0].PadRight(2, 'X')[..2];
        var part2 = words[1].PadRight(2, 'X')[..2];
        return (part1 + part2)[..4];
    }

    /// <summary>
    /// Genera el código del cluster en formato 2α + 4n (6 chars total).
    /// Si no hay cluster: CX0000.
    /// Si hay cluster: usa la abreviatura pre-calculada (2 chars) + número con padding 4.
    /// El parámetro clusterAbreviatura debe ser la abreviatura almacenada en la entidad Cluster (ej. "LA", "CN").
    /// </summary>
    internal static string GenerateClusterCode(string? clusterAbreviatura, int clusterNumero)
    {
        if (string.IsNullOrWhiteSpace(clusterAbreviatura))
            return "CX0000";

        // Tomar máximo 2 letras de la abreviatura, padding X a la derecha si es necesario
        var letters = ExtractLettersOnly(clusterAbreviatura).ToUpperInvariant();
        var abrev = letters.PadRight(2, 'X')[..2];
        var numPart = clusterNumero.ToString("D4");
        return abrev + numPart;
    }

    /// <summary>
    /// Genera el código de trayectoria:
    /// Original → vacío
    /// ST → "ST" (primero) o "ST2", "ST3"... (segundo en adelante)
    /// G → "G" (primero) o "G2", "G3"...
    /// P → "P", PR → "PR", ML → "ML"
    /// </summary>
    internal static string GenerateTrayectoriaCode(TipoTrayectoria trayectoria, int consecutivoTrayectoria)
    {
        return trayectoria switch
        {
            TipoTrayectoria.O => string.Empty,
            TipoTrayectoria.ST => consecutivoTrayectoria <= 1 ? "ST" : $"ST{consecutivoTrayectoria}",
            TipoTrayectoria.G => consecutivoTrayectoria <= 1 ? "G" : $"G{consecutivoTrayectoria}",
            TipoTrayectoria.P => "P",
            TipoTrayectoria.PR => "PR",
            TipoTrayectoria.ML => "ML",
            _ => string.Empty
        };
    }

    /// <summary>Extrae solo letras del string (elimina números, espacios, guiones, etc.)</summary>
    private static string ExtractLettersOnly(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (char.IsLetter(c))
                sb.Append(c);
        }
        return sb.ToString();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
