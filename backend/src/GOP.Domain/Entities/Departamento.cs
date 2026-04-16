namespace GOP.Domain.Entities;

public sealed class Departamento : CatalogEntity
{
    public string CodigoDane { get; set; } = string.Empty;

    public ICollection<Municipio> Municipios { get; set; } = new List<Municipio>();
}
