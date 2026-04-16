namespace GOP.Application.Features.Catalogs.Queries.GetCampos;

public sealed record CampoItemDto(
    int Id,
    string Nombre,
    int ContratoId
);
