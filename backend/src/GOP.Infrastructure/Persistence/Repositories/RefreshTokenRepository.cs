using GOP.Application.Common.Interfaces;
using GOP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GOP.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository(GopDbContext context) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct)
        => await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token, ct);

    public async Task AddAsync(RefreshToken rt, CancellationToken ct)
        => await context.RefreshTokens.AddAsync(rt, ct);

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct)
    {
        var activeTokens = await context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var rt in activeTokens)
            rt.Revoke(replacedByToken: null);

        // Cambios trackeados — SaveChangesAsync del caller los persiste
    }
}