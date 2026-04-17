using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Features.Wells.Queries.GetWellHistory;

public sealed record GetWellHistoryQuery(Guid WellId)
    : IRequest<Result<IReadOnlyList<TransitionHistoryItemDto>>>;
