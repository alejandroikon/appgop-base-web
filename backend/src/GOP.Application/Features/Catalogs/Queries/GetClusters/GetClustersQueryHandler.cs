using GOP.Application.Common.Interfaces;
using GOP.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GOP.Application.Features.Catalogs.Queries.GetClusters;

internal sealed class GetClustersQueryHandler(
    IApplicationDbContext dbContext
) : IRequestHandler<GetClustersQuery, Result<List<ClusterItemDto>>>
{
    public async Task<Result<List<ClusterItemDto>>> Handle(
        GetClustersQuery request, CancellationToken cancellationToken)
    {
        var clusters = await dbContext.Clusters
            .AsNoTracking()
            .Where(c => c.CampoId == request.CampoId)
            .OrderBy(c => c.Nombre)
            .Select(c => new ClusterItemDto(c.Id, c.Nombre, c.Abreviatura, c.CampoId))
            .ToListAsync(cancellationToken);

        return Result.Success(clusters);
    }
}
