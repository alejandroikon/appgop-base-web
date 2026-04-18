namespace GOP.Domain.Enums;

/// <summary>
/// Estados del ciclo de vida de un pozo V2.0.
/// El bloqueo post-Forma-101 se controla con el flag Forma101Radicada en la entidad Well.
/// </summary>
public enum WellStatus
{
    Borrador,   // Datos parciales, sin UWI
    Creado      // Datos completos, UWI generado
}
