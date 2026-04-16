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
/// Reemplaza SQL Server por InMemory sin interceptor de auditoría.
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

            // Suprimir Serilog
            services.RemoveAll<ILoggerFactory>();
            services.AddLogging(logging => logging.AddConsole());
        });
    }
}
