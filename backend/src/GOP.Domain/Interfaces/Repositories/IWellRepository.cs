using GOP.Domain.Entities;

namespace GOP.Domain.Interfaces.Repositories;

public interface IWellRepository
{
    void Add(Well well);
    Task<Well?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUwiAsync(string uwi, CancellationToken cancellationToken = default);
}
