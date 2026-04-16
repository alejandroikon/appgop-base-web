using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Features.Catalogs.Queries.GetClusters;

public sealed record GetClustersQuery(int CampoId) : IRequest<Result<List<ClusterItemDto>>>;
