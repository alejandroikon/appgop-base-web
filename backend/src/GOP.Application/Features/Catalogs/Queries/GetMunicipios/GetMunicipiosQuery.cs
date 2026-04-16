using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Features.Catalogs.Queries.GetMunicipios;

public sealed record GetMunicipiosQuery(int DepartamentoId) : IRequest<Result<List<MunicipioItemDto>>>;
