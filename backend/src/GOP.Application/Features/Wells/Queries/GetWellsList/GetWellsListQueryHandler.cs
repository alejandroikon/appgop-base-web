using GOP.Application.Common;
using GOP.Application.Common.Interfaces;
using GOP.Domain.Common;
using GOP.Domain.Enums;
using GOP.Domain.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GOP.Application.Features.Wells.Queries.GetWellsList;

internal sealed class GetWellsListQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser
) : IRequestHandler<GetWellsListQuery, Result<PagedList<WellListItemDto>>>
{
    private static readonly HashSet<string> AllowedSortFields =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "nombrePozo", "operadora", "contrato", "estado", "createdAt", "uwi"
        };

    public async Task<Result<PagedList<WellListItemDto>>> Handle(
        GetWellsListQuery request, CancellationToken cancellationToken)
    {
        var isAdminOrAuditor = currentUser.IsInRole("ADMIN") || currentUser.IsInRole("AUDITOR");

        var query = dbContext.Wells.AsNoTracking();
        if (isAdminOrAuditor)
            query = query.IgnoreQueryFilters();

        if (request.ContratoId.HasValue)
            query = query.Where(w => w.ContratoId == request.ContratoId.Value);

        if (!string.IsNullOrWhiteSpace(request.Estado) &&
            Enum.TryParse<WellStatus>(request.Estado, ignoreCase: true, out var estadoEnum))
        {
            query = query.Where(w => w.Estado == estadoEnum);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(w =>
                w.NombrePozo.Contains(search) ||
                w.Denominacion.Contains(search) ||
                (w.Uwi != null && w.Uwi.Contains(search)) ||
                w.Operadora.Contains(search));
        }

        if (request.CampoId.HasValue)
            query = query.Where(w => w.CampoId == request.CampoId.Value);

        if (!string.IsNullOrWhiteSpace(request.Denominacion))
        {
            var denom = request.Denominacion.Trim();
            query = query.Where(w => w.Denominacion.Contains(denom));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var sortDir = request.SortDir?.ToLowerInvariant() == "desc" ? "desc" : "asc";
        var sortBy = request.SortBy;

        if (!string.IsNullOrWhiteSpace(sortBy) && AllowedSortFields.Contains(sortBy))
        {
            query = (sortBy.ToLowerInvariant(), sortDir) switch
            {
                ("nombrepozo", "asc") => query.OrderBy(w => w.NombrePozo),
                ("nombrepozo", _) => query.OrderByDescending(w => w.NombrePozo),
                ("operadora", "asc") => query.OrderBy(w => w.Operadora),
                ("operadora", _) => query.OrderByDescending(w => w.Operadora),
                ("estado", "asc") => query.OrderBy(w => w.Estado),
                ("estado", _) => query.OrderByDescending(w => w.Estado),
                ("createdat", "asc") => query.OrderBy(w => w.CreatedAt),
                ("createdat", _) => query.OrderByDescending(w => w.CreatedAt),
                _ => query.OrderBy(w => w.NombrePozo)
            };
        }
        else
        {
            query = query.OrderByDescending(w => w.CreatedAt);
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var wells = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = wells.Select(w => new WellListItemDto(
            Id: w.Id,
            NombrePozo: w.NombrePozo,
            Operadora: w.Operadora,
            Contrato: w.Contrato,
            Campo: w.Campo,
            Clasificacion: w.Clasificacion.ToString().ToUpperInvariant(),
            SubClasificacion: w.SubClasificacion?.ToString(),
            Estado: w.Estado.ToString().ToUpperInvariant(),
            Uwi: w.Uwi,
            CreatedAt: w.CreatedAt
        )).ToList();

        return Result.Success(new PagedList<WellListItemDto>(items, totalCount, page, pageSize));
    }
}
