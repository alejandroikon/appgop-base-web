namespace GOP.Domain.Entities;

public sealed class Campo : CatalogEntity
{
    public int ContratoId { get; set; }
    public Contrato Contrato { get; set; } = null!;

    public ICollection<Cluster> Clusters { get; set; } = new List<Cluster>();
}
