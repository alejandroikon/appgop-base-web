using GOP.Domain.Entities;
using GOP.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GOP.Infrastructure.Persistence.Repositories;

internal sealed class WellRepository(GOP.Infrastructure.Persistence.GopDbContext context) : IWellRepository
{
    public void Add(Well well) => context.Wells.Add(well);

    public void Update(Well well) => context.Wells.Update(well);

    public async Task<Well?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Wells.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<bool> ExistsByUwiAsync(
        string uwi, Guid? excludeWellId, CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters: UWI debe ser único globalmente, no por tenant (ED-05)
        var query = context.Wells.IgnoreQueryFilters()
            .Where(w => !w.IsDeleted && w.Uwi == uwi);
        if (excludeWellId.HasValue)
            query = query.Where(w => w.Id != excludeWellId.Value);
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string nombrePozo, int tenantId, Guid? excludeWellId, CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters: nombre debe ser único por tenant, pero necesitamos
        // bypass del filter global para que el check funcione desde cualquier contexto
        var query = context.Wells.IgnoreQueryFilters()
            .Where(w => !w.IsDeleted && w.NombrePozo == nombrePozo && w.TenantId == tenantId);
        if (excludeWellId.HasValue)
            query = query.Where(w => w.Id != excludeWellId.Value);
        return await query.AnyAsync(cancellationToken);
    }
}
