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

        // ─── Identificación ──────────────────────────────────────────────────
        builder.Property(w => w.Operadora)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.NombrePozo)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.Uwi)
            .HasMaxLength(50);

        // ─── Contrato (desnormalizado) ────────────────────────────────────────
        builder.Property(w => w.Contrato)
            .HasMaxLength(200);

        builder.Property(w => w.TipoContrato)
            .HasMaxLength(50);

        builder.Property(w => w.Cuenca)
            .HasMaxLength(200);

        // ─── Campo (nullable) ─────────────────────────────────────────────────
        builder.Property(w => w.Campo)
            .HasMaxLength(200);

        // ─── Datos Técnicos ───────────────────────────────────────────────────
        builder.Property(w => w.Denominacion)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(w => w.Consecutivo)
            .IsRequired();

        builder.Property(w => w.TipoTrayectoria)
            .HasConversion<string>()
            .HasMaxLength(5);

        builder.Property(w => w.Clasificacion)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(w => w.SubClasificacion)
            .HasConversion<string?>()
            .HasMaxLength(5);

        builder.Property(w => w.TipoUbicacion)
            .HasConversion<string>()
            .HasMaxLength(15);

        builder.Property(w => w.TipoAngulo)
            .HasConversion<string>()
            .HasMaxLength(2);

        builder.Property(w => w.TipoObjetivo)
            .HasConversion<string>()
            .HasMaxLength(5);

        builder.Property(w => w.TipoTerminacion)
            .HasConversion<string>()
            .HasMaxLength(5);

        // ─── Estado ───────────────────────────────────────────────────────────
        builder.Property(w => w.Estado)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(w => w.Forma101Radicada)
            .HasDefaultValue(false);

        // ─── Ubicación (aplanada) ─────────────────────────────────────────────
        builder.Property(w => w.Departamento)
            .HasMaxLength(200);

        builder.Property(w => w.CodigoDaneDpto)
            .HasMaxLength(2);

        builder.Property(w => w.Municipio)
            .HasMaxLength(200);

        builder.Property(w => w.CodigoDaneMpio)
            .HasMaxLength(3);

        builder.Property(w => w.Cluster)
            .HasMaxLength(200);

        // ─── Multi-tenancy ────────────────────────────────────────────────────
        builder.Property(w => w.TenantId)
            .IsRequired();

        // ─── Índices ──────────────────────────────────────────────────────────
        builder.HasIndex(w => w.TenantId)
            .HasDatabaseName("IX_Wells_TenantId");

        // UWI único global (excluyendo nulls y registros eliminados)
        builder.HasIndex(w => w.Uwi)
            .IsUnique()
            .HasFilter("[Uwi] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName("IX_Wells_Uwi_Global");

        // Nombre único por tenant (excluyendo registros eliminados)
        builder.HasIndex(w => new { w.TenantId, w.NombrePozo })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Wells_TenantId_NombrePozo");

        // Índice por estado para filtros frecuentes
        builder.HasIndex(w => new { w.TenantId, w.Estado })
            .HasDatabaseName("IX_Wells_TenantId_Estado");

        // ─── Soft delete ──────────────────────────────────────────────────────
        builder.HasQueryFilter(w => !w.IsDeleted);

        // ─── FKs (restrict para no cascade en datos master) ───────────────────
        builder.HasOne<Contrato>()
            .WithMany()
            .HasForeignKey(w => w.ContratoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Campo>()
            .WithMany()
            .HasForeignKey(w => w.CampoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Departamento>()
            .WithMany()
            .HasForeignKey(w => w.DepartamentoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Municipio>()
            .WithMany()
            .HasForeignKey(w => w.MunicipioId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Cluster>()
            .WithMany()
            .HasForeignKey(w => w.ClusterId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
