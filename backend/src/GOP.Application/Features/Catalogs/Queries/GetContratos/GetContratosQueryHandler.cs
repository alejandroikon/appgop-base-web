using GOP.Application.Common.Interfaces;
using GOP.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GOP.Application.Features.Catalogs.Queries.GetContratos;

internal sealed class GetContratosQueryHandler(
    IApplicationDbContext dbContext
) : IRequestHandler<GetContratosQuery, Result<List<ContratoItemDto>>>
{
    public async Task<Result<List<ContratoItemDto>>> Handle(
        GetContratosQuery request, CancellationToken cancellationToken)
    {
        var contratos = await dbContext.Contratos
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new ContratoItemDto(c.Id, c.Nombre, c.Tipo, c.Cuenca))
            .ToListAsync(cancellationToken);

        return Result.Success(contratos);
    }
}
