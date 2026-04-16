namespace GOP.Domain.Entities;

/// <summary>
/// Clase base para entidades de catálogo con Id entero (no Guid como Entity).
/// </summary>
public abstract class CatalogEntity
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
