namespace GOP.Application.Features.Catalogs.Queries.GetMunicipios;

public sealed record MunicipioItemDto(
    int Id,
    string Nombre,
    int DepartamentoId,
    string CodigoDane
);
