using GOP.Application.Common.Interfaces;
using GOP.Domain.Common;
using GOP.Domain.Entities;
using GOP.Domain.Enums;
using GOP.Domain.Errors;
using GOP.Domain.Interfaces;
using GOP.Domain.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GOP.Application.Features.Wells.Commands.TransitionWell;

internal sealed class TransitionWellCommandHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IUwiGenerator uwiGenerator
) : IRequestHandler<TransitionWellCommand, Result<TransitionResultDto>>
{
    public async Task<Result<TransitionResultDto>> Handle(
        TransitionWellCommand request, CancellationToken cancellationToken)
    {
        // 1. Buscar pozo por ID (multi-tenant filtrado por query filter global)
        var well = await dbContext.Wells
            .FirstOrDefaultAsync(w => w.Id == request.WellId, cancellationToken);

        if (well is null)
            return Result.Failure<TransitionResultDto>(DomainErrors.Well.NotFoundById(request.WellId));

        // 2. Parsear Action string → TransitionAction enum
        if (!TryParseAction(request.Action, out var action))
            return Result.Failure<TransitionResultDto>(DomainErrors.Well.InvalidTransition(request.Action, well.Estado.ToString()));

        var estadoAnterior = well.Estado;

        // 3. Si acción es ENVIAR y pozo no tiene UWI → generar UWI
        string? generatedUwi = null;
        if (action == TransitionAction.Enviar && well.Uwi is null)
        {
            var uwiResult = await uwiGenerator.GenerateAsync(well, cancellationToken);
            if (uwiResult.IsFailure)
                return Result.Failure<TransitionResultDto>(uwiResult.Error);

            generatedUwi = uwiResult.Value;

            // 4. Verificar unicidad de UWI
            var exists = await dbContext.Wells.AnyAsync(w => w.Uwi == generatedUwi, cancellationToken);
            if (exists)
                return Result.Failure<TransitionResultDto>(DomainErrors.Well.DuplicateUwi);
        }

        // 5. Aplicar transición en la entidad de dominio
        var transitionResult = well.ApplyTransition(action, currentUser.Role, request.Comment);
        if (transitionResult.IsFailure)
            return Result.Failure<TransitionResultDto>(transitionResult.Error);

        // 6. Si UWI fue generado → asignar al pozo
        if (generatedUwi is not null)
            well.SetUwi(generatedUwi);

        // 7. Registrar historial de transición
        var history = WellTransitionHistory.Create(
            wellId: well.Id,
            from: estadoAnterior,
            to: well.Estado,
            action: action,
            comment: request.Comment,
            userId: currentUser.UserId,
            userName: currentUser.Name,
            userRole: currentUser.Role);

        dbContext.WellTransitionHistory.Add(history);

        // 8. Persistir
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 9. Retornar DTO de respuesta
        var dto = new TransitionResultDto(
            Id: well.Id,
            Estado: well.Estado.ToString(),
            EstadoAnterior: estadoAnterior.ToString(),
            Uwi: well.Uwi,
            Action: action.ToString(),
            Comment: request.Comment,
            TransitionedAt: history.CreatedAt,
            TransitionedBy: currentUser.Name);

        return Result.Success(dto);
    }

    private static bool TryParseAction(string action, out TransitionAction result)
    {
        result = default;
        return action.ToUpperInvariant() switch
        {
            "ENVIAR"      => SetAndReturn(TransitionAction.Enviar, out result),
            "APROBAR_UWI" => SetAndReturn(TransitionAction.AprobarUwi, out result),
            "DEVOLVER"    => SetAndReturn(TransitionAction.Devolver, out result),
            "FISCALIZAR"  => SetAndReturn(TransitionAction.Fiscalizar, out result),
            _             => false
        };
    }

    private static bool SetAndReturn(TransitionAction value, out TransitionAction result)
    {
        result = value;
        return true;
    }
}
