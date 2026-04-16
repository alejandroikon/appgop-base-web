using GOP.Application.Common.Interfaces;
using GOP.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GOP.Infrastructure.Persistence;

public sealed class GopDbContext(DbContextOptions<GopDbContext> options)
    : DbContext(options), IApplicationDbContext, IUnitOfWork
{
    // Los DbSet<T> de entidades se agregan en features de negocio
    // según CONSTITUTION.backend.md RT-002

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GopDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
