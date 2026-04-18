using GOP.Application.Common.Interfaces;
using GOP.Domain.Common;
using GOP.Domain.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GOP.Application.Features.Wells.Commands.TransitionWell;

// TODO-ITER-9.3: Este handler es legacy (flujo V1). El flujo V2.0 usa CreateWellCommand(FINALIZE)
// y UpdateWellCommand(FINALIZE) en su lugar. Mantener solo para compatibilidad con datos existentes.
internal sealed class TransitionWellCommandHandler(
    IApplicationDbContext dbContext
) : IRequestHandler<TransitionWellCommand, Result<TransitionResultDto>>
{
    public async Task<Result<TransitionResultDto>> Handle(
        TransitionWellCommand request, CancellationToken cancellationToken)
    {
        var well = await dbContext.Wells
            .FirstOrDefaultAsync(w => w.Id == request.WellId, cancellationToken);

        if (well is null)
            return Result.Failure<TransitionResultDto>(DomainErrors.Well.NotFoundById(request.WellId));

        // TODO-ITER-9.3: La máquina de estados V1 fue eliminada en V2.0.
        // Para compatibilidad, solo permitimos operaciones básicas.
        return Result.Failure<TransitionResultDto>(new Error(
            "Well.LegacyTransitionNotSupported",
            "El flujo de transición V1 no está disponible en V2.0. Use POST /wells con action=FINALIZE."));
    }
}
