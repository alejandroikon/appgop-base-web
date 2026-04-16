using GOP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GOP.Infrastructure.Persistence.Configurations;

internal sealed class ContratoConfiguration : IEntityTypeConfiguration<Contrato>
{
    public void Configure(EntityTypeBuilder<Contrato> builder)
    {
        builder.ToTable("Contratos");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Tipo)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Cuenca)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasMany(c => c.Campos)
            .WithOne(ca => ca.Contrato)
            .HasForeignKey(ca => ca.ContratoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed data
        builder.HasData(
            new Contrato { Id = 1, Nombre = "Contrato E&P Llanos", Tipo = "E&P", Cuenca = "Llanos Orientales" },
            new Contrato { Id = 2, Nombre = "Contrato E&P Piedemonte", Tipo = "E&P", Cuenca = "Piedemonte Llanero" },
            new Contrato { Id = 3, Nombre = "Contrato E&P Magdalena", Tipo = "E&P", Cuenca = "Valle Medio del Magdalena" }
        );
    }
}
