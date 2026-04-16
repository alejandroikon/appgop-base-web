namespace GOP.Domain.Entities;

public sealed class Cluster : CatalogEntity
{
    public int CampoId { get; set; }
    public Campo Campo { get; set; } = null!;
}
