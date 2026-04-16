namespace GOP.Domain.Entities;

public sealed class Municipio : CatalogEntity
{
    public int DepartamentoId { get; set; }
    public Departamento Departamento { get; set; } = null!;
    public string CodigoDane { get; set; } = string.Empty;
}
