using System.Text;
using GOP.API.Extensions;
using GOP.API.Middleware;
using GOP.Application;
using GOP.Infrastructure;
using GOP.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;

// Bootstrap logger para capturar errores de arranque
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando GOP 360° API...");

    var builder = WebApplication.CreateBuilder(args);

    // T-INFRA-15a: Key Vault (Staging/Production) — carga secretos antes de configurar servicios
    builder.AddKeyVaultIfConfigured();

    // Serilog — leer configuración de appsettings.json (incluye App Insights sink en Staging)
    builder.Host.UseSerilog((context, loggerConfiguration) =>
        loggerConfiguration.ReadFrom.Configuration(context.Configuration));

    // DI por capa
    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration);

    // JWT Authentication
    var jwtSettings = builder.Configuration
        .GetSection(JwtSettings.SectionName)
        .Get<JwtSettings>()!;

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };
        });

    builder.Services.AddAuthorization();

    // Controllers
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Swagger / NSwag — disponible en todos los ambientes (facilita validación en staging)
    builder.Services.AddOpenApiDocument(config =>
    {
        config.Title = "GOP 360° API";
        config.Version = "v1";
        config.Description = "API Backend de GOP 360° — ANH Colombia";
    });

    // Health Checks
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services
        .AddHealthChecks()
        .AddSqlServer(
            connectionString ?? string.Empty,
            name: "sqlserver",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["db", "sql"])
        .AddDbContextCheck<GOP.Infrastructure.Persistence.GopDbContext>(
            name: "efcore",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["db", "ef"]);

    // T-INFRA-15b: CORS — Dev + Staging (*.netlify.app + localhost)
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DevPolicy", policy =>
            policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials());

        // RN-SEC-02: Staging/Production CORS
        options.AddPolicy("StagingPolicy", policy =>
            policy.SetIsOriginAllowed(origin =>
                    new Uri(origin).Host.EndsWith(".netlify.app", StringComparison.OrdinalIgnoreCase) ||
                    origin.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase) ||
                    origin.StartsWith("https://localhost:", StringComparison.OrdinalIgnoreCase))
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials());
    });

    var app = builder.Build();

    // T-INFRA-15c: Migrations + Seed on startup (idempotente — RN-INFRA-06, RN-INFRA-07)
    await app.ApplyMigrationsAndSeedAsync();

    // Middleware pipeline (el orden es crítico)
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseCors("DevPolicy");
    }
    else
    {
        app.UseCors("StagingPolicy");
    }

    // Swagger disponible siempre para facilitar validación E2E en staging
    app.UseOpenApi();
    app.UseSwaggerUi(settings =>
    {
        settings.Path = "/swagger";
        settings.DocumentPath = "/swagger/v1/swagger.json";
    });

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    Log.Information("GOP 360° API iniciada correctamente en {Environment}.",
        app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación terminó inesperadamente.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Necesario para WebApplicationFactory en tests de integración
public partial class Program { }
