using GOP.Application.Common.Interfaces;
using GOP.Domain.Entities;
using GOP.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GOP.Application.Tests.Common;

/// <summary>
/// DbContext en memoria para tests unitarios de Application.
/// Implementa IApplicationDbContext e IUnitOfWork sin depender de GOP.Infrastructure.
/// </summary>
public sealed class TestDbContext(DbContextOptions<TestDbContext> options)
    : DbContext(options), IApplicationDbContext, IUnitOfWork
{
    public DbSet<Well> Wells => Set<Well>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<Campo> Campos => Set<Campo>();
    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<Municipio> Municipios => Set<Municipio>();
    public DbSet<Cluster> Clusters => Set<Cluster>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configuración mínima para tests: enums como string, owned entity
        modelBuilder.Entity<Well>(w =>
        {
            w.Property(x => x.TipoTrayectoria).HasConversion<string>();
            w.Property(x => x.Clasificacion).HasConversion<string>();
            w.Property(x => x.TipoUbicacion).HasConversion<string>();
            w.Property(x => x.TipoAngulo).HasConversion<string>();
            w.Property(x => x.TipoObjetivo).HasConversion<string>();
            w.Property(x => x.TipoTerminacion).HasConversion<string>();
            w.Property(x => x.Estado).HasConversion<string>();
            w.OwnsOne(x => x.Location);
        });

        base.OnModelCreating(modelBuilder);
    }

    public static TestDbContext Create(string dbName = "")
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(string.IsNullOrEmpty(dbName) ? Guid.NewGuid().ToString() : dbName)
            .Options;
        return new TestDbContext(options);
    }
}
