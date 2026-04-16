namespace GOP.Domain.Entities;

public sealed class Contrato : CatalogEntity
{
    public string Tipo { get; set; } = string.Empty;
    public string Cuenca { get; set; } = string.Empty;

    public ICollection<Campo> Campos { get; set; } = new List<Campo>();
}
