using FluentValidation;

namespace GOP.Application.Features.Wells.Commands.CreateWell;

internal sealed class CreateWellCommandValidator : AbstractValidator<CreateWellCommand>
{
    private static readonly string[] ValidTipoTrayectoria = ["ST", "P", "PR", "ML", "G", "O"];
    private static readonly string[] ValidClasificacion = ["EXPLORATORIO", "DESARROLLO", "ESTRATIGRAFICO"];
    private static readonly string[] ValidTipoUbicacion = ["CONTINENTAL", "COSTA_FUERA"];
    private static readonly string[] ValidTipoAngulo = ["H", "V", "D"];
    private static readonly string[] ValidTipoObjetivo = ["PH", "I", "M", "D"];
    private static readonly string[] ValidTipoTerminacion = ["CD", "LC", "LR", "GP", "CC", "OH", "O"];

    public CreateWellCommandValidator()
    {
        RuleFor(x => x.ContratoId)
            .GreaterThan(0).WithMessage("Debe seleccionar un contrato válido.");

        RuleFor(x => x.CampoId)
            .GreaterThan(0).WithMessage("Debe seleccionar un campo válido.");

        RuleFor(x => x.TipoTrayectoria)
            .NotEmpty().WithMessage("El tipo de trayectoria es requerido.")
            .Must(v => ValidTipoTrayectoria.Contains(v))
            .WithMessage("Tipo de trayectoria no válido.");

        RuleFor(x => x.Clasificacion)
            .NotEmpty().WithMessage("La clasificación es requerida.")
            .Must(v => ValidClasificacion.Contains(v))
            .WithMessage("Clasificación no válida.");

        RuleFor(x => x.Denominacion)
            .NotEmpty().WithMessage("La denominación es requerida.")
            .MaximumLength(50).WithMessage("La denominación no puede exceder 50 caracteres.")
            .Matches(@"^[A-Za-záéíóúÁÉÍÓÚñÑ ]+$")
            .WithMessage("La denominación solo puede contener letras y espacios.");

        RuleFor(x => x.Consecutivo)
            .NotEmpty().WithMessage("El consecutivo es requerido.")
            .Matches(@"^\d{2}$")
            .WithMessage("El consecutivo debe ser exactamente 2 dígitos numéricos.");

        RuleFor(x => x.TipoUbicacion)
            .NotEmpty().WithMessage("El tipo de ubicación es requerido.")
            .Must(v => ValidTipoUbicacion.Contains(v))
            .WithMessage("Tipo de ubicación no válido.");

        RuleFor(x => x.TipoAngulo)
            .NotEmpty().WithMessage("El tipo de ángulo es requerido.")
            .Must(v => ValidTipoAngulo.Contains(v))
            .WithMessage("Tipo de ángulo no válido.");

        RuleFor(x => x.TipoObjetivo)
            .NotEmpty().WithMessage("El tipo de objetivo es requerido.")
            .Must(v => ValidTipoObjetivo.Contains(v))
            .WithMessage("Tipo de objetivo no válido.");

        RuleFor(x => x.TipoTerminacion)
            .NotEmpty().WithMessage("El tipo de terminación es requerido.")
            .Must(v => ValidTipoTerminacion.Contains(v))
            .WithMessage("Tipo de terminación no válido.");

        RuleFor(x => x.DepartamentoId)
            .GreaterThan(0).WithMessage("Debe seleccionar un departamento válido.");

        RuleFor(x => x.MunicipioId)
            .GreaterThan(0).WithMessage("Debe seleccionar un municipio válido.");
    }
}
