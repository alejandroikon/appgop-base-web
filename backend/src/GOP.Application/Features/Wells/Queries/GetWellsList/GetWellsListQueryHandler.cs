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
            "nombrePozo", "operadora", "contrato", "createdAt"
        };

    public async Task<Result<PagedList<WellListItemDto>>> Handle(
        GetWellsListQuery request, CancellationToken cancellationToken)
    {
        // Multi-tenant: ADMIN y AUDITOR ven todos los pozos, los demás solo los de su tenant
        var isAdminOrAuditor = currentUser.IsInRole("ADMIN") || currentUser.IsInRole("AUDITOR");

        var query = dbContext.Wells.AsNoTracking();

        if (isAdminOrAuditor)
            query = query.IgnoreQueryFilters();

        // Filtros opcionales
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
                w.Denominacion.Contains(search));
        }

        // Contar total antes de paginar
        var totalCount = await query.CountAsync(cancellationToken);

        // Ordenamiento (whitelist para evitar inyección de nombres de columna)
        var sortBy = request.SortBy;
        var sortDir = request.SortDir?.ToLowerInvariant() == "desc" ? "desc" : "asc";

        if (!string.IsNullOrWhiteSpace(sortBy) && AllowedSortFields.Contains(sortBy))
        {
            query = (sortBy.ToLowerInvariant(), sortDir) switch
            {
                ("nombrepozo", "asc") => query.OrderBy(w => w.NombrePozo),
                ("nombrepozo", _) => query.OrderByDescending(w => w.NombrePozo),
                ("operadora", "asc") => query.OrderBy(w => w.Operadora),
                ("operadora", _) => query.OrderByDescending(w => w.Operadora),
                ("createdat", "asc") => query.OrderBy(w => w.CreatedAt),
                ("createdat", _) => query.OrderByDescending(w => w.CreatedAt),
                _ => query.OrderBy(w => w.NombrePozo)
            };
        }
        else
        {
            query = query.OrderByDescending(w => w.CreatedAt);
        }

        // Paginación
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : (request.PageSize > 100 ? 100 : request.PageSize);

        var wells = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Resolver nombres de catálogos para los pozos de la página
        var contratoIds = wells.Select(w => w.ContratoId).Distinct().ToList();
        var campoIds = wells.Select(w => w.CampoId).Distinct().ToList();

        var contratos = await dbContext.Contratos
            .AsNoTracking()
            .Where(c => contratoIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Nombre, cancellationToken);

        var campos = await dbContext.Campos
            .AsNoTracking()
            .Where(c => campoIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Nombre, cancellationToken);

        var items = wells.Select(w => new WellListItemDto(
            Id: w.Id,
            NombrePozo: w.NombrePozo,
            Operadora: w.Operadora,
            Contrato: contratos.TryGetValue(w.ContratoId, out var cn) ? cn : string.Empty,
            Campo: campos.TryGetValue(w.CampoId, out var camp) ? camp : string.Empty,
            Clasificacion: w.Clasificacion.ToString(),
            Estado: w.Estado.ToString(),
            CreatedAt: w.CreatedAt
        )).ToList();

        return Result.Success(new PagedList<WellListItemDto>(items, totalCount, page, pageSize));
    }
}
