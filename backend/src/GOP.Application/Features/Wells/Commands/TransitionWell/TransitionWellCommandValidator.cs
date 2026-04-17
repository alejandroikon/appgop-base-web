using FluentValidation;

namespace GOP.Application.Features.Wells.Commands.TransitionWell;

internal sealed class TransitionWellCommandValidator : AbstractValidator<TransitionWellCommand>
{
    private static readonly HashSet<string> ValidActions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "ENVIAR", "APROBAR_UWI", "DEVOLVER", "FISCALIZAR"
        };

    public TransitionWellCommandValidator()
    {
        RuleFor(x => x.WellId)
            .NotEmpty()
            .WithMessage("El ID del pozo es requerido.");

        RuleFor(x => x.Action)
            .NotEmpty()
            .WithMessage("La acción de transición es requerida.")
            .Must(a => ValidActions.Contains(a))
            .WithMessage("La acción no es válida. Valores permitidos: ENVIAR, APROBAR_UWI, DEVOLVER, FISCALIZAR.");

        RuleFor(x => x.Comment)
            .NotEmpty()
            .WithMessage("El motivo de devolución es requerido.")
            .MinimumLength(10)
            .WithMessage("El motivo de devolución debe tener al menos 10 caracteres.")
            .When(x => string.Equals(x.Action, "DEVOLVER", StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.Comment)
            .MaximumLength(500)
            .WithMessage("El comentario no puede exceder 500 caracteres.")
            .When(x => x.Comment is not null);
    }
}
