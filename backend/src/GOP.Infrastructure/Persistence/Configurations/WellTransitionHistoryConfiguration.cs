using GOP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GOP.Infrastructure.Persistence.Configurations;

internal sealed class WellTransitionHistoryConfiguration : IEntityTypeConfiguration<WellTransitionHistory>
{
    public void Configure(EntityTypeBuilder<WellTransitionHistory> builder)
    {
        builder.ToTable("WellTransitionHistory");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.WellId)
            .IsRequired();

        builder.Property(h => h.FromState)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(h => h.ToState)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(h => h.Action)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(h => h.Comment)
            .HasMaxLength(500);

        builder.Property(h => h.PerformedByUserId)
            .IsRequired();

        builder.Property(h => h.PerformedByName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(h => h.PerformedByRole)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(h => h.CreatedAt)
            .IsRequired();

        builder.HasIndex(h => h.WellId)
            .HasDatabaseName("IX_WellTransitionHistory_WellId");

        builder.HasOne<Well>()
            .WithMany()
            .HasForeignKey(h => h.WellId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
