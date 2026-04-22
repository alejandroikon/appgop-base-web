using GOP.Domain.Entities;
using GOP.Infrastructure.Persistence;
using GOP.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace GOP.API.Tests.Fixtures;

/// <summary>
/// Factory compartida entre clases de test API.
/// Reemplaza SQL Server por EF Core InMemory sin interceptor de auditoría.
///
/// NOTA (BE-M02 — aceptado con conocimiento): Se usa EF InMemory en lugar de Testcontainers SQL Server.
/// Limitaciones conocidas:
///   - No aplica FK enforcement → inserciones con FK inválidas no fallan.
///   - No replica índices únicos reales → DuplicateUwi no se valida a nivel DB.
///   - Los global query filters (IsDeleted, TenantId) se comportan igual → no hay diferencia.
/// Impacto real: los tests de integración API no detectan violaciones de constraints de SQL Server.
/// Decisión: aceptable para el MVP dado que los constraints están cubiertos en GOP.Domain.Tests
/// y GOP.Application.Tests. Migración a Testcontainers queda pendiente para iteraciones P2.
/// </summary>
public sealed class GopTestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Eliminar TODAS las descriptors que involucren GopDbContext
            // (incluyendo el registro via factory de IDbContextOptionsConfiguration)
            var toRemove = services.Where(d =>
                d.ServiceType.FullName != null &&
                (d.ServiceType.FullName.Contains("GopDbContext") ||
                 d.ServiceType.FullName.Contains("DbContextOptions") ||
                 d.ServiceType == typeof(AuditableEntityInterceptor)))
                .ToList();

            foreach (var d in toRemove)
                services.Remove(d);

            // Sin interceptor en tests InMemory (no hay SQL Server real)
            services.AddDbContext<GopDbContext>(options =>
                options.UseInMemoryDatabase("TestDb_Wells_Integration"));

            // Registrar IApplicationDbContext e IUnitOfWork apuntando al nuevo DbContext
            services.AddScoped<GOP.Application.Common.Interfaces.IApplicationDbContext>(
                sp => sp.GetRequiredService<GopDbContext>());
            services.AddScoped<GOP.Domain.Interfaces.IUnitOfWork>(
                sp => sp.GetRequiredService<GopDbContext>());

            // Sembrar 4 dev users para que los tests de integración Auth encuentren
            // usuarios válidos sin depender del UserSeeder real (que requiere SQL Server).
            // Los hashes corresponden a: Admin123*, Super123*, Oper123*, Audit123*
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GopDbContext>();
            db.Database.EnsureCreated();

            db.Users.AddRange(
                User.Create(
                    "admin@gop.co",
                    "$2a$10$lbfNGZ8k62bJIAziu3nLZed7ZVIb2Cu7I67RHaEPqzqJorEY/CH7G",
                    "Administrador ANH"),
                User.Create(
                    "supervisor@gop.co",
                    "$2a$10$B2cnoTofYSWvjVdA2sWwtenCJZ3PqO9MIJzjfIPrre40YYqoSVGku",
                    "Supervisor Ecopetrol"),
                User.Create(
                    "operador@gop.co",
                    "$2a$10$KrWbtKA8w4Lz94lBSit/S.WUBazvTwNIrkBX662lVZ/srCz0Q7M..",
                    "Operador Ecopetrol"),
                User.Create(
                    "auditor@gop.co",
                    "$2a$10$tOEjBdrDvvlnDeIueaOLbOxqxgnVAocPqat1jmmIUuls8bXKJND/C",
                    "Auditor ANH"));
            db.SaveChanges();

            // Suprimir Serilog
            services.RemoveAll<ILoggerFactory>();
            services.AddLogging(logging => logging.AddConsole());
        });
    }
}
