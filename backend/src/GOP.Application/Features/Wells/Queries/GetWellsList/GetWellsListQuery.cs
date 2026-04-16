using GOP.Application.Common;
using GOP.Application.Features.Wells.Queries.GetWellsList;
using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Features.Wells.Queries.GetWellsList;

public sealed record GetWellsListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? SortBy = null,
    string? SortDir = null,
    int? ContratoId = null,
    string? Estado = null
) : IRequest<Result<PagedList<WellListItemDto>>>;
