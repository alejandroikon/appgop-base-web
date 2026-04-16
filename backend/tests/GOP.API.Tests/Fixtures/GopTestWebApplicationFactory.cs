using GOP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace GOP.API.Tests.Fixtures;

/// <summary>
/// Factory compartida entre clases de test API para evitar el conflicto de Serilog
/// "logger already frozen" cuando múltiples WebApplicationFactory se crean en el mismo proceso.
/// Usada via [Collection("ApiIntegrationTests")] en los test classes.
/// </summary>
public sealed class GopTestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Reemplazar SQL Server por InMemory para tests sin Docker
            services.RemoveAll<DbContextOptions<GopDbContext>>();
            services.RemoveAll<GopDbContext>();

            services.AddDbContext<GopDbContext>(options =>
                options.UseInMemoryDatabase("TestDb_Integration"));

            // Suprimir Serilog para evitar "logger already frozen"
            // cuando esta factory comparte proceso con otros factories xUnit
            services.RemoveAll<ILoggerFactory>();
            services.AddLogging(logging => logging.AddConsole());
        });
    }
}
