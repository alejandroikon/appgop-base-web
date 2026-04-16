using GOP.Application.Common.Interfaces;
using GOP.Domain.Interfaces;
using GOP.Domain.Interfaces.Services;
using GOP.Infrastructure.Identity;
using GOP.Infrastructure.Persistence;
using GOP.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GOP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "La cadena de conexión 'DefaultConnection' no está configurada.");

        // Registrar interceptor como Scoped (accede a ICurrentUserService que es Scoped)
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<GopDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(connectionString);

            var interceptor = serviceProvider.GetRequiredService<AuditableEntityInterceptor>();
            options.AddInterceptors(interceptor);
        });

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<GopDbContext>());

        services.AddScoped<IUnitOfWork>(sp =>
            sp.GetRequiredService<GopDbContext>());

        // JWT Settings (for Infrastructure services)
        services.Configure<JwtSettings>(
            configuration.GetSection(JwtSettings.SectionName));

        // JwtTokenOptions (for Application handlers, bound from same config section)
        services.Configure<JwtTokenOptions>(
            configuration.GetSection(JwtSettings.SectionName));

        // Identity services
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IUserSeedStore, UserSeedStoreAdapter>();
        services.AddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();

        return services;
    }
}
