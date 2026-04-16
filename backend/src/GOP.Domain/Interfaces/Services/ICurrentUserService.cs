namespace GOP.Domain.Interfaces.Services;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string Email { get; }
    string Name { get; }
    string Role { get; }
    int TenantId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
