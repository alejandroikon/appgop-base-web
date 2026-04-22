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
    public DbSet<WellTransitionHistory> WellTransitionHistory => Set<WellTransitionHistory>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Well>(w =>
        {
            w.Property(x => x.TipoTrayectoria).HasConversion<string>();
            w.Property(x => x.Clasificacion).HasConversion<string>();
            w.Property(x => x.SubClasificacion).HasConversion<string?>();
            w.Property(x => x.TipoUbicacion).HasConversion<string>();
            w.Property(x => x.TipoAngulo).HasConversion<string>();
            w.Property(x => x.TipoObjetivo).HasConversion<string>();
            w.Property(x => x.TipoTerminacion).HasConversion<string>();
            w.Property(x => x.Estado).HasConversion<string>();
            // Soft delete — no query filter in-memory para tests (más simple)
        });

        modelBuilder.Entity<WellTransitionHistory>(h =>
        {
            h.Property(x => x.FromState).HasConversion<string>();
            h.Property(x => x.ToState).HasConversion<string>();
            h.Property(x => x.Action).HasConversion<string>();
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
