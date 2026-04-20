using GOP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GOP.Infrastructure.Persistence.Configurations;

// T-INFRA-21: HasData mínimo removido — datos completos DANE via DbSeeder (1123 municipios)
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

        // HasData vacío — seed real en DbSeeder con datos DANE completos
    }
}
