namespace GOP.Application.Common.Interfaces;

public sealed record RefreshTokenEntry(
    Guid UserId,
    string Token,
    DateTime ExpiresAt,
    bool IsUsed);

public interface IRefreshTokenStore
{
    void Store(string token, Guid userId, DateTime expiresAt);
    RefreshTokenEntry? TryConsume(string token);
}
