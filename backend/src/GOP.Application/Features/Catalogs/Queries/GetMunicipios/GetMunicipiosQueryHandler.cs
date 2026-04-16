using GOP.Application.Common.Interfaces;
using GOP.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GOP.Application.Features.Catalogs.Queries.GetMunicipios;

internal sealed class GetMunicipiosQueryHandler(
    IApplicationDbContext dbContext
) : IRequestHandler<GetMunicipiosQuery, Result<List<MunicipioItemDto>>>
{
    public async Task<Result<List<MunicipioItemDto>>> Handle(
        GetMunicipiosQuery request, CancellationToken cancellationToken)
    {
        var municipios = await dbContext.Municipios
            .AsNoTracking()
            .Where(m => m.DepartamentoId == request.DepartamentoId)
            .OrderBy(m => m.Nombre)
            .Select(m => new MunicipioItemDto(m.Id, m.Nombre, m.DepartamentoId, m.CodigoDane))
            .ToListAsync(cancellationToken);

        return Result.Success(municipios);
    }
}
