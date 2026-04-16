using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Features.Wells.Queries.GetWellById;

public sealed record GetWellByIdQuery(Guid WellId) : IRequest<Result<WellDetailDto>>;
