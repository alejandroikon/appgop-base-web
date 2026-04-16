using GOP.Application.Common.Interfaces;
using GOP.Domain.Interfaces;
using GOP.Domain.Interfaces.Services;
using GOP.Infrastructure.Persistence;
using GOP.Infrastructure.Services;
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

        services.AddDbContext<GopDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<GopDbContext>());

        services.AddScoped<IUnitOfWork>(sp =>
            sp.GetRequiredService<GopDbContext>());

        services.AddScoped<ICurrentUserService, CurrentUserServiceStub>();

        return services;
    }
}
