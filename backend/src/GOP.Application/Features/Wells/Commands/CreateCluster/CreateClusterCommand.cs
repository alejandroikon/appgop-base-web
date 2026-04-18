using GOP.Application.Features.Catalogs.Queries.GetClusters;
using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Features.Wells.Commands.CreateCluster;

public sealed record CreateClusterCommand(
    string Nombre,
    int CampoId
) : IRequest<Result<ClusterItemDto>>;
