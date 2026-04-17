using GOP.Domain.Entities;
using GOP.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GOP.Infrastructure.Persistence.Repositories;

internal sealed class WellRepository(GOP.Infrastructure.Persistence.GopDbContext context) : IWellRepository
{
    public void Add(Well well) => context.Wells.Add(well);

    public async Task<Well?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Wells.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<bool> ExistsByUwiAsync(string uwi, CancellationToken cancellationToken = default)
        => await context.Wells.AnyAsync(w => w.Uwi == uwi, cancellationToken);
}
