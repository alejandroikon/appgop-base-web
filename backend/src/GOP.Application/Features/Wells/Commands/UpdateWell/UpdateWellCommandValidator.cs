using FluentValidation;
using System.Text.RegularExpressions;

namespace GOP.Application.Features.Wells.Commands.UpdateWell;

internal sealed class UpdateWellCommandValidator : AbstractValidator<UpdateWellCommand>
{
    private static readonly string[] ValidActions = ["SAVE", "FINALIZE"];
    private static readonly string[] ValidTipoTrayectoria = ["ST", "P", "PR", "ML", "G", "O"];
    private static readonly string[] ValidClasificacion = ["EXPLORATORIO", "DESARROLLO", "ESTRATIGRAFICO"];
    private static readonly string[] ValidSubClasificacion = ["A3", "A2a", "A2b", "A2c", "A1"];
    private static readonly string[] ValidTipoUbicacion = ["CONTINENTAL", "COSTA_FUERA"];
    private static readonly string[] ValidTipoAngulo = ["H", "V", "D"];
    private static readonly string[] ValidTipoObjetivo = ["PH", "I", "M", "D", "C", "GT", "O"];
    private static readonly string[] ValidTipoTerminacion = ["CD", "LC", "LR", "GP", "CC", "OH", "O"];
    private static readonly Regex DenominacionRegex = new(@"^[A-Za-záéíóúÁÉÍÓÚüÜñÑ0-9 \-]+$", RegexOptions.Compiled);

    public UpdateWellCommandValidator()
    {
        RuleFor(x => x.WellId)
            .NotEmpty().WithMessage("El ID del pozo es requerido.");

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("La acción es requerida (SAVE o FINALIZE).")
            .Must(v => ValidActions.Contains(v?.ToUpperInvariant()))
            .WithMessage("La acción debe ser SAVE o FINALIZE.");

        When(x => x.Denominacion != null, () =>
        {
            RuleFor(x => x.Denominacion!)
                .MaximumLength(50).WithMessage("La denominación no puede superar 50 caracteres.")
                .Matches(DenominacionRegex).WithMessage("Solo se permiten letras, números, espacios y guiones.");
        });

        When(x => x.Consecutivo.HasValue, () =>
        {
            RuleFor(x => x.Consecutivo!.Value)
                .InclusiveBetween(1, 9999).WithMessage("El consecutivo debe estar entre 1 y 9999.");
        });

        When(x => x.TipoTrayectoria != null, () =>
        {
            RuleFor(x => x.TipoTrayectoria!)
                .Must(v => ValidTipoTrayectoria.Contains(v?.ToUpperInvariant()))
                .WithMessage($"Tipo de trayectoria inválido. Valores válidos: {string.Join(", ", ValidTipoTrayectoria)}");
        });

        When(x => x.Clasificacion != null, () =>
        {
            RuleFor(x => x.Clasificacion!)
                .Must(v => ValidClasificacion.Contains(v?.ToUpperInvariant()))
                .WithMessage($"Clasificación inválida. Valores válidos: {string.Join(", ", ValidClasificacion)}");
        });

        When(x => x.SubClasificacion != null, () =>
        {
            RuleFor(x => x.SubClasificacion!)
                .Must(v => ValidSubClasificacion.Contains(v))
                .WithMessage($"Sub-clasificación inválida. Valores válidos: {string.Join(", ", ValidSubClasificacion)}");
        });

        When(x => x.TipoAngulo != null, () =>
        {
            RuleFor(x => x.TipoAngulo!)
                .Must(v => ValidTipoAngulo.Contains(v?.ToUpperInvariant()))
                .WithMessage($"Tipo de ángulo inválido. Valores válidos: {string.Join(", ", ValidTipoAngulo)}");
        });

        When(x => x.TipoObjetivo != null, () =>
        {
            RuleFor(x => x.TipoObjetivo!)
                .Must(v => ValidTipoObjetivo.Contains(v?.ToUpperInvariant()))
                .WithMessage($"Tipo de objetivo inválido. Valores válidos: {string.Join(", ", ValidTipoObjetivo)}");
        });

        When(x => x.TipoTerminacion != null, () =>
        {
            RuleFor(x => x.TipoTerminacion!)
                .Must(v => ValidTipoTerminacion.Contains(v?.ToUpperInvariant()))
                .WithMessage($"Tipo de terminación inválido. Valores válidos: {string.Join(", ", ValidTipoTerminacion)}");
        });
    }
}
