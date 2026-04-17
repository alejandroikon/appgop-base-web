using GOP.Domain.Common;
using System.Globalization;
using System.Text;

namespace GOP.Domain.ValueObjects;

public sealed record Uwi
{
    public string Value { get; }

    private Uwi(string value) => Value = value;

    /// <summary>
    /// Genera un UWI con el formato: CO-{daneDpto}-{daneMpio}-{DENOMINACION}-{consecutivo}-{trayectoria}
    /// Normaliza la denominación a UPPERCASE sin diacríticos (á→A, ñ→N, etc.)
    /// </summary>
    public static Result<Uwi> Create(
        string daneDpto,
        string daneMpio,
        string denominacion,
        string consecutivo,
        string trayectoria)
    {
        var normalizedDenominacion = NormalizeDenominacion(denominacion);
        var uwiValue = $"CO-{daneDpto}-{daneMpio}-{normalizedDenominacion}-{consecutivo}-{trayectoria}";

        if (uwiValue.Length > 50)
            return Result.Failure<Uwi>(new Error(
                "Uwi.TooLong",
                $"El UWI generado '{uwiValue}' excede el máximo de 50 caracteres."));

        return Result.Success(new Uwi(uwiValue));
    }

    /// <summary>Elimina diacríticos y convierte a UPPERCASE</summary>
    private static string NormalizeDenominacion(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    public override string ToString() => Value;
}
