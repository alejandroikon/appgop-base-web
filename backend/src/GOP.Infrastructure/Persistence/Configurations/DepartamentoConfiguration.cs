using GOP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GOP.Infrastructure.Persistence.Configurations;

// T-INFRA-20: HasData mínimo removido — datos completos DANE via DbSeeder (1123 municipios)
// El seeder carga departamentos.json y municipios.json idempotentemente en cada startup
internal sealed class DepartamentoConfiguration : IEntityTypeConfiguration<Departamento>
{
    public void Configure(EntityTypeBuilder<Departamento> builder)
    {
        builder.ToTable("Departamentos");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.CodigoDane)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasMany(d => d.Municipios)
            .WithOne(m => m.Departamento)
            .HasForeignKey(m => m.DepartamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        // HasData vacío — seed real en DbSeeder con datos DANE completos
    }
}
