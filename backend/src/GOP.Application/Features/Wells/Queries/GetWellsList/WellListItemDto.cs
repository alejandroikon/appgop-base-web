namespace GOP.Application.Features.Wells.Queries.GetWellsList;

public sealed record WellListItemDto(
    Guid Id,
    string NombrePozo,
    string Operadora,
    string Contrato,
    string Campo,
    string Clasificacion,
    string Estado,
    DateTime CreatedAt
);
