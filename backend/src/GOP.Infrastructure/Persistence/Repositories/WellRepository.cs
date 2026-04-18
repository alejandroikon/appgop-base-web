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
        var query = context.Wells.Where(w => w.Uwi == uwi);
        if (excludeWellId.HasValue)
            query = query.Where(w => w.Id != excludeWellId.Value);
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string nombrePozo, int tenantId, Guid? excludeWellId, CancellationToken cancellationToken = default)
    {
        var query = context.Wells
            .Where(w => w.NombrePozo == nombrePozo && w.TenantId == tenantId);
        if (excludeWellId.HasValue)
            query = query.Where(w => w.Id != excludeWellId.Value);
        return await query.AnyAsync(cancellationToken);
    }
}
