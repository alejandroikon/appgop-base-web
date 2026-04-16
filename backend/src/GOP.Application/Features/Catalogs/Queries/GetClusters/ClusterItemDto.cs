namespace GOP.Application.Features.Catalogs.Queries.GetClusters;

public sealed record ClusterItemDto(
    int Id,
    string Nombre,
    int CampoId
);
