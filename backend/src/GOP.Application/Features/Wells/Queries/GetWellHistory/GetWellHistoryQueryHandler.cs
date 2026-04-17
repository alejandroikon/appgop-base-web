using AutoMapper;
using GOP.Application.Common.Interfaces;
using GOP.Domain.Common;
using GOP.Domain.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GOP.Application.Features.Wells.Queries.GetWellHistory;

internal sealed class GetWellHistoryQueryHandler(
    IApplicationDbContext dbContext,
    IMapper mapper
) : IRequestHandler<GetWellHistoryQuery, Result<IReadOnlyList<TransitionHistoryItemDto>>>
{
    public async Task<Result<IReadOnlyList<TransitionHistoryItemDto>>> Handle(
        GetWellHistoryQuery request, CancellationToken cancellationToken)
    {
        // 1. Verificar que el pozo existe (respeta query filter de soft-delete y multi-tenant)
        var wellExists = await dbContext.Wells
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WellId, cancellationToken);

        if (!wellExists)
            return Result.Failure<IReadOnlyList<TransitionHistoryItemDto>>(
                DomainErrors.Well.NotFoundById(request.WellId));

        // 2. Consultar historial ordenado por fecha descendente
        var history = await dbContext.WellTransitionHistory
            .AsNoTracking()
            .Where(h => h.WellId == request.WellId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);

        // 3. Mapear a DTOs
        var dtos = mapper.Map<IReadOnlyList<TransitionHistoryItemDto>>(history);
        return Result.Success(dtos);
    }
}
