using GOP.Application.Common.Interfaces;
using GOP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GOP.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(GopDbContext context) : IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
        => await context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
        => await context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task AddAsync(User user, CancellationToken ct)
        => await context.Users.AddAsync(user, ct);

    public void Update(User user)
        => context.Users.Update(user);

    public async Task<bool> ExistsAsync(string email, CancellationToken ct)
        => await context.Users.AnyAsync(u => u.Email == email, ct);
}