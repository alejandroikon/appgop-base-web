using GOP.Domain.Entities;
using GOP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GOP.Infrastructure.Persistence.Configurations;

internal sealed class WellConfiguration : IEntityTypeConfiguration<Well>
{
    public void Configure(EntityTypeBuilder<Well> builder)
    {
        builder.ToTable("Wells");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Operadora)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.TipoContrato)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(w => w.Cuenca)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.Denominacion)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(w => w.Consecutivo)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(w => w.NombrePozo)
            .IsRequired()
            .HasMaxLength(300);

        // Enums almacenados como string
        builder.Property(w => w.TipoTrayectoria)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(w => w.Clasificacion)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(w => w.TipoUbicacion)
            .HasConversion<string>()
            .HasMaxLength(15);

        builder.Property(w => w.TipoAngulo)
            .HasConversion<string>()
            .HasMaxLength(5);

        builder.Property(w => w.TipoObjetivo)
            .HasConversion<string>()
            .HasMaxLength(5);

        builder.Property(w => w.TipoTerminacion)
            .HasConversion<string>()
            .HasMaxLength(5);

        builder.Property(w => w.Estado)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Multi-tenant
        builder.Property(w => w.TenantId)
            .IsRequired();

        builder.HasIndex(w => w.TenantId)
            .HasDatabaseName("IX_Wells_TenantId");

        // Soft delete — query filter global
        builder.HasQueryFilter(w => !w.IsDeleted);

        // Owned entity: WellLocation (columnas en la misma tabla Wells)
        builder.OwnsOne(w => w.Location, location =>
        {
            location.Property(l => l.DepartamentoId)
                .HasColumnName("DepartamentoId")
                .IsRequired();

            location.Property(l => l.MunicipioId)
                .HasColumnName("MunicipioId")
                .IsRequired();

            location.Property(l => l.ClusterId)
                .HasColumnName("ClusterId");

            location.Property(l => l.CodigoDaneDpto)
                .HasColumnName("CodigoDaneDpto")
                .IsRequired()
                .HasMaxLength(10);

            location.Property(l => l.CodigoDaneMpio)
                .HasColumnName("CodigoDaneMpio")
                .IsRequired()
                .HasMaxLength(10);
        });

        // FKs a catálogos (sin cascade para evitar delete en cascada de datos master)
        builder.HasOne<Contrato>()
            .WithMany()
            .HasForeignKey(w => w.ContratoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Campo>()
            .WithMany()
            .HasForeignKey(w => w.CampoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
