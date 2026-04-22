// T-INFRA-14: Migrate on startup — EF Core idempotent migrations + seed
// RN-INFRA-06: applied in startup so every deploy is self-contained

using GOP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GOP.API.Extensions;

public static class MigrationExtension
{
    public static async Task ApplyMigrationsAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            var db = scope.ServiceProvider.GetRequiredService<GopDbContext>();

            logger.LogInformation("Aplicando migraciones EF Core...");
            await db.Database.MigrateAsync();
            logger.LogInformation("Migraciones aplicadas exitosamente.");

            var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
            logger.LogInformation("Ejecutando seed DANE...");
            await seeder.SeedAsync();
            logger.LogInformation("Seed completado.");

            var userSeeder = scope.ServiceProvider.GetRequiredService<UserSeeder>();
            logger.LogInformation("Ejecutando seed de usuarios...");
            await userSeeder.SeedAsync();
            logger.LogInformation("Seed de usuarios completado.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error durante migraciones o seed. La aplicación continuará pero puede estar en estado inconsistente.");
            // No re-throw: let health check report the actual DB state
        }
    }
}
