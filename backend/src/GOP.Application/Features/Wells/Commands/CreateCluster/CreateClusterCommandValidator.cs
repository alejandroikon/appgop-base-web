using FluentValidation;

namespace GOP.Application.Features.Wells.Commands.CreateCluster;

internal sealed class CreateClusterCommandValidator : AbstractValidator<CreateClusterCommand>
{
    public CreateClusterCommandValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del cluster es requerido.")
            .MaximumLength(100).WithMessage("El nombre del cluster no puede superar 100 caracteres.");

        RuleFor(x => x.CampoId)
            .GreaterThan(0).WithMessage("Debe especificar un campo válido.");
    }
}
