using GOP.Domain.Entities;

namespace GOP.Domain.Interfaces.Repositories;

public interface IWellRepository
{
    void Add(Well well);
    void Update(Well well);
    Task<Well?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUwiAsync(string uwi, Guid? excludeWellId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string nombrePozo, int tenantId, Guid? excludeWellId, CancellationToken cancellationToken = default);
}
