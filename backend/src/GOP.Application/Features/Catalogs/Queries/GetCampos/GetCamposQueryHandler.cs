using GOP.Application.Common.Interfaces;
using GOP.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GOP.Application.Features.Catalogs.Queries.GetCampos;

internal sealed class GetCamposQueryHandler(
    IApplicationDbContext dbContext
) : IRequestHandler<GetCamposQuery, Result<List<CampoItemDto>>>
{
    public async Task<Result<List<CampoItemDto>>> Handle(
        GetCamposQuery request, CancellationToken cancellationToken)
    {
        var campos = await dbContext.Campos
            .AsNoTracking()
            .Where(c => c.ContratoId == request.ContratoId)
            .OrderBy(c => c.Nombre)
            .Select(c => new CampoItemDto(c.Id, c.Nombre, c.ContratoId))
            .ToListAsync(cancellationToken);

        return Result.Success(campos);
    }
}
