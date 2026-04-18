namespace GOP.Domain.Enums;

/// <summary>
/// Sub-clasificación de pozos exploratorios según resolución ANH. Solo aplica cuando
/// Clasificacion == Exploratorio.
/// </summary>
public enum SubClasificacionExploratoria
{
    A3,   // Área nueva (wildcat)
    A2a,  // Yacimiento nuevo, campo nuevo
    A2b,  // Yacimiento nuevo, campo conocido
    A2c,  // Yacimiento conocido, campo conocido
    A1    // Pozo de Avanzada (appraisal)
}
