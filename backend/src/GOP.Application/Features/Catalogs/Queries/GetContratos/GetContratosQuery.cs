using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Features.Catalogs.Queries.GetContratos;

public sealed record GetContratosQuery : IRequest<Result<List<ContratoItemDto>>>;
