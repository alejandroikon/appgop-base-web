using GOP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GOP.Infrastructure.Persistence.Configurations;

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

        // Seed data
        builder.HasData(
            new Departamento { Id = 1, Nombre = "Meta", CodigoDane = "50" },
            new Departamento { Id = 2, Nombre = "Casanare", CodigoDane = "85" },
            new Departamento { Id = 3, Nombre = "Santander", CodigoDane = "68" }
        );
    }
}
