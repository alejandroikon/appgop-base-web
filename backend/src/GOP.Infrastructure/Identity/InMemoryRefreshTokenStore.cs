using GOP.Application.Common.Interfaces;
using System.Collections.Concurrent;

namespace GOP.Infrastructure.Identity;

internal sealed class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly ConcurrentDictionary<string, RefreshTokenEntry> _store = new();

    public void Store(string token, Guid userId, DateTime expiresAt)
    {
        var entry = new RefreshTokenEntry(userId, token, expiresAt, IsUsed: false);
        _store[token] = entry;
    }

    public RefreshTokenEntry? TryConsume(string token)
    {
        if (!_store.TryGetValue(token, out var entry))
            return null;

        // Mark as used atomically
        var usedEntry = entry with { IsUsed = true };
        _store[token] = usedEntry;

        return entry; // Return original (caller checks IsUsed on original state)
    }
}
