namespace GOP.Domain.Entities;

/// <summary>
/// Owned entity que almacena la ubicación geográfica del pozo.
/// No hereda de Entity (sin Guid propio), vive en la misma tabla que Well.
/// </summary>
public sealed class WellLocation
{
    public int DepartamentoId { get; set; }
    public int MunicipioId { get; set; }
    public int? ClusterId { get; set; }
    public string CodigoDaneDpto { get; set; } = string.Empty;
    public string CodigoDaneMpio { get; set; } = string.Empty;
}
