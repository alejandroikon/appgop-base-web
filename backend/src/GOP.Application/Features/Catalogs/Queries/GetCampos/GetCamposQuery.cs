using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Features.Catalogs.Queries.GetCampos;

public sealed record GetCamposQuery(int ContratoId) : IRequest<Result<List<CampoItemDto>>>;
