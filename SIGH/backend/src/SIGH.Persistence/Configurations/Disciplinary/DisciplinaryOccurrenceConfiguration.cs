using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGH.Domain.Disciplinary.Constants;
using SIGH.Domain.Disciplinary.Entities;

namespace SIGH.Persistence.Configurations.Disciplinary;

public class DisciplinaryOccurrenceConfiguration : IEntityTypeConfiguration<DisciplinaryOccurrence>
{
    public void Configure(EntityTypeBuilder<DisciplinaryOccurrence> builder)
    {
        builder.ToTable("DisciplinaryOccurrences");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .ValueGeneratedNever();

        builder.Property(o => o.DisciplinaryCaseId)
            .IsRequired();

        builder.Property(o => o.InfractionTypeId)
            .IsRequired();

        builder.Property(o => o.OccurrenceDate)
            .IsRequired();

        builder.Property(o => o.ReportedAt)
            .IsRequired();

        builder.Property(o => o.ReportedByUserId)
            .IsRequired();

        builder.Property(o => o.Description)
            .IsRequired()
            .HasMaxLength(DisciplinaryDomainConstants.DescriptionMaxLength);

        builder.Property(o => o.Location)
            .HasMaxLength(DisciplinaryDomainConstants.LocationMaxLength);

        builder.Property(o => o.Severity)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(o => o.ConfidentialityLevel)
            .IsRequired()
            .HasConversion<int>();

        // Relationship
        builder.HasOne<InfractionType>()
            .WithMany()
            .HasForeignKey(o => o.InfractionTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(o => o.DisciplinaryCaseId);
        builder.HasIndex(o => o.InfractionTypeId);
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.Severity);
        builder.HasIndex(o => o.OccurrenceDate);
        builder.HasIndex(o => o.ReportedAt);
        builder.HasIndex(o => o.ConfidentialityLevel);
        builder.HasIndex(o => o.IsDeleted);

        // Composite Indexes
        builder.HasIndex(o => new { o.DisciplinaryCaseId, o.Status });
        builder.HasIndex(o => new { o.DisciplinaryCaseId, o.OccurrenceDate });
    }
}
