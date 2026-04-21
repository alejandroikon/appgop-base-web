using GOP.Application.Common.Interfaces;
using GOP.Domain.Entities;
using GOP.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GOP.Infrastructure.Persistence;

public sealed class UserSeeder(
    IUserRepository userRepo,
    IUnitOfWork unitOfWork,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<UserSeeder> logger)
{
    private static readonly (string Key, string DefaultEmail, string DefaultFullName)[] _adminSlots =
    [
        ("ExecAdmin", "alejandro.gutierrez@interkont.co", "Alejandro Gutiérrez"),
        ("OpAdmin",   "admin@interkont.co",               "Admin Operativo"),
    ];

    // Datos estáticos de dev users (espejo de SeedUsers.cs — hashes BCrypt conocidos)
    private static readonly (Guid Id, string Email, string Hash, string FullName)[] _devUsers =
    [
        (Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
         "admin@gop.co",
         "$2a$10$lbfNGZ8k62bJIAziu3nLZed7ZVIb2Cu7I67RHaEPqzqJorEY/CH7G",
         "Administrador ANH"),

        (Guid.Parse("550e8400-e29b-41d4-a716-446655440002"),
         "supervisor@gop.co",
         "$2a$10$B2cnoTofYSWvjVdA2sWwtenCJZ3PqO9MIJzjfIPrre40YYqoSVGku",
         "Supervisor Ecopetrol"),

        (Guid.Parse("550e8400-e29b-41d4-a716-446655440003"),
         "operador@gop.co",
         "$2a$10$KrWbtKA8w4Lz94lBSit/S.WUBazvTwNIrkBX662lVZ/srCz0Q7M..",
         "Operador Ecopetrol"),

        (Guid.Parse("550e8400-e29b-41d4-a716-446655440004"),
         "auditor@gop.co",
         "$2a$10$tOEjBdrDvvlnDeIueaOLbOxqxgnVAocPqat1jmmIUuls8bXKJND/C",
         "Auditor ANH"),
    ];

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var seedSection = configuration.GetSection("SeedUsers");

        if (!seedSection.Exists())
        {
            logger.LogWarning("SeedUsers section not configured. User seed skipped.");

            if (environment.IsDevelopment())
                await SeedDevUsersAsync(ct);

            return;
        }

        // Sección existe → iterar slots de admin configurados
        foreach (var (key, defaultEmail, defaultFullName) in _adminSlots)
        {
            var slot = seedSection.GetSection(key);

            var email    = (slot["Email"]    ?? defaultEmail).Trim().ToLowerInvariant();
            var password = slot["Password"];
            var fullName = slot["FullName"]  ?? defaultFullName;

            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException(
                    $"SeedUsers:{key}:Password not configured. Seed aborted.");

            if (await userRepo.ExistsAsync(email, ct))
            {
                logger.LogInformation("Usuario {Email} ya existe, omitiendo seed.", email);
                continue;
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = User.Create(email, hash, fullName);
            await userRepo.AddAsync(user, ct);
            logger.LogInformation("Usuario {Email} sembrado exitosamente.", email);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task SeedDevUsersAsync(CancellationToken ct)
    {
        foreach (var (_, email, hash, fullName) in _devUsers)
        {
            if (await userRepo.ExistsAsync(email, ct))
            {
                logger.LogInformation("Usuario dev {Email} ya existe, omitiendo seed.", email);
                continue;
            }

            var user = User.Create(email, hash, fullName);
            await userRepo.AddAsync(user, ct);
            logger.LogInformation("Usuario dev {Email} sembrado exitosamente.", email);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}