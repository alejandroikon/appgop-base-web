using GOP.Domain.Entities;

namespace GOP.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    void Update(User user);
    Task<bool> ExistsAsync(string email, CancellationToken ct);
}
