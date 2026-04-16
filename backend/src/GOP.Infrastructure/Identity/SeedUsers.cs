using GOP.Application.Features.Auth.Queries.GetCurrentUser;

namespace GOP.Infrastructure.Identity;

internal static class SeedUsers
{
    private sealed record SeedUserEntry(
        Guid Id,
        string Email,
        string PasswordHash,
        string Name,
        string Role,
        string TenantId,
        string TenantName);

    private static readonly IReadOnlyList<SeedUserEntry> _users =
    [
        new(
            Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
            "admin@gop.co",
            "$2a$10$lbfNGZ8k62bJIAziu3nLZed7ZVIb2Cu7I67RHaEPqzqJorEY/CH7G",
            "Administrador ANH",
            "ADMIN",
            "1",
            "Agencia Nacional de Hidrocarburos"),

        new(
            Guid.Parse("550e8400-e29b-41d4-a716-446655440002"),
            "supervisor@gop.co",
            "$2a$10$B2cnoTofYSWvjVdA2sWwtenCJZ3PqO9MIJzjfIPrre40YYqoSVGku",
            "Supervisor Ecopetrol",
            "SUPERVISOR",
            "2",
            "Ecopetrol S.A."),

        new(
            Guid.Parse("550e8400-e29b-41d4-a716-446655440003"),
            "operador@gop.co",
            "$2a$10$KrWbtKA8w4Lz94lBSit/S.WUBazvTwNIrkBX662lVZ/srCz0Q7M..",
            "Operador Ecopetrol",
            "OPERADOR",
            "2",
            "Ecopetrol S.A."),

        new(
            Guid.Parse("550e8400-e29b-41d4-a716-446655440004"),
            "auditor@gop.co",
            "$2a$10$tOEjBdrDvvlnDeIueaOLbOxqxgnVAocPqat1jmmIUuls8bXKJND/C",
            "Auditor ANH",
            "AUDITOR",
            "1",
            "Agencia Nacional de Hidrocarburos"),
    ];

    public static UserProfileDto? FindByEmail(string normalizedEmail) =>
        _users
            .Where(u => u.Email == normalizedEmail)
            .Select(ToDto)
            .FirstOrDefault();

    public static UserProfileDto? GetById(Guid userId) =>
        _users
            .Where(u => u.Id == userId)
            .Select(ToDto)
            .FirstOrDefault();

    public static bool VerifyPassword(Guid userId, string password)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        if (user is null) return false;
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }

    private static UserProfileDto ToDto(SeedUserEntry u) =>
        new(u.Id, u.Email, u.Name, u.Role, u.TenantId, u.TenantName);
}
