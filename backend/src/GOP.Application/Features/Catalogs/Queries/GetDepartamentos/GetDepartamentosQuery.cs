using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Features.Catalogs.Queries.GetDepartamentos;

public sealed record GetDepartamentosQuery : IRequest<Result<List<DepartamentoItemDto>>>;
