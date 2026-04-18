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
            .Matches(@"^[A-Za-záéíóúÁÉÍÓÚüÜñÑ0-9 \-]+$")
                .WithMessage("La denominación solo acepta letras, números, espacios y guiones.");

        RuleFor(x => x.Consecutivo)
            .InclusiveBetween(1, 9999).WithMessage("El consecutivo debe estar entre 1 y 9999.");
    }
}
