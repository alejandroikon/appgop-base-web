using GOP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GOP.Infrastructure.Persistence.Configurations;

internal sealed class CampoConfiguration : IEntityTypeConfiguration<Campo>
{
    public void Configure(EntityTypeBuilder<Campo> builder)
    {
        builder.ToTable("Campos");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasMany(c => c.Clusters)
            .WithOne(cl => cl.Campo)
            .HasForeignKey(cl => cl.CampoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed data (6 campos, 2 por contrato)
        builder.HasData(
            new Campo { Id = 1, Nombre = "Campo Rubiales", ContratoId = 1 },
            new Campo { Id = 2, Nombre = "Campo Quifa", ContratoId = 1 },
            new Campo { Id = 3, Nombre = "Campo Cusiana", ContratoId = 2 },
            new Campo { Id = 4, Nombre = "Campo Cupiagua", ContratoId = 2 },
            new Campo { Id = 5, Nombre = "Campo Lisama", ContratoId = 3 },
            new Campo { Id = 6, Nombre = "Campo La Cira", ContratoId = 3 }
        );
    }
}
