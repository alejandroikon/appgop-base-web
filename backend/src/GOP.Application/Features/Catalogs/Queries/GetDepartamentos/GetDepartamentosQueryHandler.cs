using GOP.Application.Common.Interfaces;
using GOP.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GOP.Application.Features.Catalogs.Queries.GetDepartamentos;

internal sealed class GetDepartamentosQueryHandler(
    IApplicationDbContext dbContext
) : IRequestHandler<GetDepartamentosQuery, Result<List<DepartamentoItemDto>>>
{
    public async Task<Result<List<DepartamentoItemDto>>> Handle(
        GetDepartamentosQuery request, CancellationToken cancellationToken)
    {
        var departamentos = await dbContext.Departamentos
            .AsNoTracking()
            .OrderBy(d => d.Nombre)
            .Select(d => new DepartamentoItemDto(d.Id, d.Nombre, d.CodigoDane))
            .ToListAsync(cancellationToken);

        return Result.Success(departamentos);
    }
}
