namespace GOP.Application.Features.Auth.Queries.GetCurrentUser;

public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string Name,
    string Role,
    string TenantId,
    string TenantName);
