using FluentValidation;

namespace GOP.Application.Features.Wells.Queries.PreviewWellName;

internal sealed class PreviewWellNameQueryValidator : AbstractValidator<PreviewWellNameQuery>
{
    public PreviewWellNameQueryValidator()
    {
        RuleFor(x => x.ContratoId)
            .GreaterThan(0).WithMessage("Debe seleccionar un contrato válido.");

        RuleFor(x => x.Denominacion)
            .NotEmpty().WithMessage("La denominación es requerida.")
            .MaximumLength(50).WithMessage("La denominación no puede exceder 50 caracteres.")
            .Matches(@"^[A-Za-záéíóúÁÉÍÓÚñÑ ]+$")
                .WithMessage("La denominación solo acepta letras y espacios.");

        RuleFor(x => x.Consecutivo)
            .NotEmpty().WithMessage("El consecutivo es requerido.")
            .Matches(@"^\d{2}$").WithMessage("El consecutivo debe ser numérico de 2 dígitos.");
    }
}
