namespace GOP.Application.Features.Catalogs.Queries.GetContratos;

public sealed record ContratoItemDto(
    int Id,
    string Nombre,
    string Tipo,
    string Cuenca
);
