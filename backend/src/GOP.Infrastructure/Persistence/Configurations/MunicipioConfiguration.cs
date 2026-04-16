using GOP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GOP.Infrastructure.Persistence.Configurations;

internal sealed class MunicipioConfiguration : IEntityTypeConfiguration<Municipio>
{
    public void Configure(EntityTypeBuilder<Municipio> builder)
    {
        builder.ToTable("Municipios");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.CodigoDane)
            .IsRequired()
            .HasMaxLength(10);

        // Seed data (6 municipios, 2 por departamento)
        builder.HasData(
            new Municipio { Id = 1, Nombre = "Puerto Gaitán", DepartamentoId = 1, CodigoDane = "50568" },
            new Municipio { Id = 2, Nombre = "Villavicencio", DepartamentoId = 1, CodigoDane = "50001" },
            new Municipio { Id = 3, Nombre = "Yopal", DepartamentoId = 2, CodigoDane = "85001" },
            new Municipio { Id = 4, Nombre = "Aguazul", DepartamentoId = 2, CodigoDane = "85010" },
            new Municipio { Id = 5, Nombre = "Barrancabermeja", DepartamentoId = 3, CodigoDane = "68081" },
            new Municipio { Id = 6, Nombre = "Bucaramanga", DepartamentoId = 3, CodigoDane = "68001" }
        );
    }
}
