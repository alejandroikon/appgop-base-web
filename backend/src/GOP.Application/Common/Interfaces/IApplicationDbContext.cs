using GOP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GOP.Application.Common.Interfaces;

/// <summary>
/// Abstracción del DbContext de aplicación. Se extiende en cada feature de negocio.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Well> Wells { get; }
    DbSet<Contrato> Contratos { get; }
    DbSet<Campo> Campos { get; }
    DbSet<Departamento> Departamentos { get; }
    DbSet<Municipio> Municipios { get; }
    DbSet<Cluster> Clusters { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
