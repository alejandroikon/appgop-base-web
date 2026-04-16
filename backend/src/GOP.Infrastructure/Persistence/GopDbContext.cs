using GOP.Application.Common.Interfaces;
using GOP.Domain.Entities;
using GOP.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GOP.Infrastructure.Persistence;

public sealed class GopDbContext(DbContextOptions<GopDbContext> options)
    : DbContext(options), IApplicationDbContext, IUnitOfWork
{
    // Wells feature
    public DbSet<Well> Wells => Set<Well>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<Campo> Campos => Set<Campo>();
    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<Municipio> Municipios => Set<Municipio>();
    public DbSet<Cluster> Clusters => Set<Cluster>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GopDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
