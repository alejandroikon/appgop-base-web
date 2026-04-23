using GOP.Application.Common.Interfaces;
using GOP.Domain.Entities;
using GOP.Domain.Interfaces;
using GOP.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace GOP.Infrastructure.Persistence;

public sealed class GopDbContext : DbContext, IApplicationDbContext, IUnitOfWork
{
    private readonly int _currentTenantId;

    public GopDbContext(
        DbContextOptions<GopDbContext> options,
        ICurrentUserService currentUserService)
        : base(options)
    {
        _currentTenantId = currentUserService.TenantId;
    }

    // Wells feature
    public DbSet<Well> Wells => Set<Well>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<Campo> Campos => Set<Campo>();
    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<Municipio> Municipios => Set<Municipio>();
    public DbSet<Cluster> Clusters => Set<Cluster>();
    public DbSet<WellTransitionHistory> WellTransitionHistory => Set<WellTransitionHistory>();

    // Users feature (007-users-persistence)
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GopDbContext).Assembly);

        // ED-05: Global query filter — tenant isolation para Wells
        // Filtra por TenantId del usuario autenticado. Si TenantId=0 (no autenticado,
        // ej. seed/migrations) no filtra nada — el filtro deja pasar todo.
        modelBuilder.Entity<Well>().HasQueryFilter(
            w => !w.IsDeleted && (_currentTenantId == 0 || w.TenantId == _currentTenantId));

        base.OnModelCreating(modelBuilder);
    }
}