namespace GOP.Domain.Entities;

public sealed class Cluster : CatalogEntity
{
    /// <summary>
    /// Abreviatura de 2 caracteres usada en la generación del UWI Fiscalizado PPDM.
    /// RN-32: formato 2α del segmento Cluster/Locación.
    /// </summary>
    public string Abreviatura { get; set; } = string.Empty;

    public int CampoId { get; set; }
    public Campo Campo { get; set; } = null!;
}
