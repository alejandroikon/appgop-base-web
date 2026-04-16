using GOP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GOP.Infrastructure.Persistence.Configurations;

internal sealed class ClusterConfiguration : IEntityTypeConfiguration<Cluster>
{
    public void Configure(EntityTypeBuilder<Cluster> builder)
    {
        builder.ToTable("Clusters");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        // Seed data (4 clusters)
        builder.HasData(
            new Cluster { Id = 1, Nombre = "Cluster Norte", CampoId = 1 },
            new Cluster { Id = 2, Nombre = "Cluster Sur", CampoId = 1 },
            new Cluster { Id = 3, Nombre = "Cluster Este", CampoId = 3 },
            new Cluster { Id = 4, Nombre = "Cluster Oeste", CampoId = 3 }
        );
    }
}
