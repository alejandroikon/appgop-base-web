using GOP.Application.Common.Interfaces;
using GOP.Domain.Common;
using GOP.Domain.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GOP.Application.Features.Wells.Queries.PreviewWellName;

internal sealed class PreviewWellNameQueryHandler(
    IApplicationDbContext dbContext
) : IRequestHandler<PreviewWellNameQuery, Result<WellNamePreviewDto>>
{
    public async Task<Result<WellNamePreviewDto>> Handle(
        PreviewWellNameQuery request, CancellationToken cancellationToken)
    {
        var contrato = await dbContext.Contratos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ContratoId, cancellationToken);

        if (contrato is null)
            return Result.Failure<WellNamePreviewDto>(DomainErrors.Contrato.NotFound);

        var nombrePozo = $"{contrato.Cuenca}-{request.Denominacion.Trim()}-{request.Consecutivo}";

        var query = dbContext.Wells
            .AsNoTracking()
            .Where(w => w.NombrePozo == nombrePozo);

        if (request.ExcludeWellId.HasValue)
            query = query.Where(w => w.Id != request.ExcludeWellId.Value);

        var exists = await query.AnyAsync(cancellationToken);

        return Result.Success(new WellNamePreviewDto(nombrePozo, !exists));
    }
}
