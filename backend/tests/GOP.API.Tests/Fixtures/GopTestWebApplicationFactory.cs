using GOP.Domain.Common;
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
            // IMPORTANTE: Los IDs deben coincidir con los hardcoded en SeedUsers (Infrastructure/Identity)
            // para que SeedUserClaimsResolver pueda resolver el Role correcto por Id.
            // Sin esto, el resolver cae al default ADMIN y rompe tests de RBAC (ej. auditor → 403).
            // Los hashes corresponden a: Admin123*, Super123*, Oper123*, Audit123*
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GopDbContext>();
            db.Database.EnsureCreated();

            db.Users.AddRange(
                SeedUser(
                    Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
                    "admin@gop.co",
                    "$2a$10$lbfNGZ8k62bJIAziu3nLZed7ZVIb2Cu7I67RHaEPqzqJorEY/CH7G",
                    "Administrador ANH"),
                SeedUser(
                    Guid.Parse("550e8400-e29b-41d4-a716-446655440002"),
                    "supervisor@gop.co",
                    "$2a$10$B2cnoTofYSWvjVdA2sWwtenCJZ3PqO9MIJzjfIPrre40YYqoSVGku",
                    "Supervisor Ecopetrol"),
                SeedUser(
                    Guid.Parse("550e8400-e29b-41d4-a716-446655440003"),
                    "operador@gop.co",
                    "$2a$10$KrWbtKA8w4Lz94lBSit/S.WUBazvTwNIrkBX662lVZ/srCz0Q7M..",
                    "Operador Ecopetrol"),
                SeedUser(
                    Guid.Parse("550e8400-e29b-41d4-a716-446655440004"),
                    "auditor@gop.co",
                    "$2a$10$tOEjBdrDvvlnDeIueaOLbOxqxgnVAocPqat1jmmIUuls8bXKJND/C",
                    "Auditor ANH"));
            db.SaveChanges();

            // Seed catálogos para tests de integración Wells (Iter 9)
            // Idempotente: solo inserta si la tabla está vacía.
            // IDs coinciden con HasData de las Configuration classes y con staging (ED-09).
            if (!db.Contratos.Any())
            {
                db.Contratos.AddRange(
                    new Contrato { Id = 1, Nombre = "Contrato E&P Llanos", Tipo = "E&P", Cuenca = "Llanos Orientales" },
                    new Contrato { Id = 2, Nombre = "Contrato E&P Piedemonte", Tipo = "E&P", Cuenca = "Piedemonte Llanero" },
                    new Contrato { Id = 3, Nombre = "Contrato E&P Magdalena", Tipo = "E&P", Cuenca = "Valle Medio del Magdalena" });

                db.Campos.AddRange(
                    new Campo { Id = 1, Nombre = "Campo Rubiales", ContratoId = 1 },
                    new Campo { Id = 2, Nombre = "Campo Quifa", ContratoId = 1 },
                    new Campo { Id = 3, Nombre = "Campo Cusiana", ContratoId = 2 });

                db.Clusters.AddRange(
                    new Cluster { Id = 1, Nombre = "Cluster Norte", Abreviatura = "CN", CampoId = 1 },
                    new Cluster { Id = 2, Nombre = "Cluster Sur", Abreviatura = "CS", CampoId = 1 });

                // IDs secuenciales alineados con staging (ED-09)
                db.Departamentos.AddRange(
                    new Departamento { Id = 1, Nombre = "Meta", CodigoDane = "50" },
                    new Departamento { Id = 2, Nombre = "Casanare", CodigoDane = "85" },
                    new Departamento { Id = 3, Nombre = "Santander", CodigoDane = "68" });

                db.Municipios.AddRange(
                    new Municipio { Id = 1, Nombre = "Puerto Gaitán", DepartamentoId = 1, CodigoDane = "50568" },
                    new Municipio { Id = 2, Nombre = "Puerto López", DepartamentoId = 1, CodigoDane = "50573" },
                    new Municipio { Id = 3, Nombre = "Tauramena", DepartamentoId = 2, CodigoDane = "85410" },
                    new Municipio { Id = 4, Nombre = "Aguazul", DepartamentoId = 2, CodigoDane = "85010" },
                    new Municipio { Id = 5, Nombre = "Barrancabermeja", DepartamentoId = 3, CodigoDane = "68081" });

                db.SaveChanges();
            }

            // Suprimir Serilog
            services.RemoveAll<ILoggerFactory>();
            services.AddLogging(logging => logging.AddConsole());
        });
    }

    /// <summary>
    /// Crea un User con Id específico mediante reflexión sobre la propiedad
    /// protected init Entity.Id. Necesario solo en tests para alinear los IDs
    /// sembrados con los de SeedUsers y permitir que el resolver de claims
    /// asigne Role/Tenant correctos.
    /// </summary>
    private static User SeedUser(Guid id, string email, string passwordHash, string fullName)
    {
        var user = User.Create(email, passwordHash, fullName);
        typeof(Entity)
            .GetProperty(nameof(Entity.Id))!
            .SetValue(user, id);
        return user;
    }
}
