using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGH.Domain.Disciplinary.Constants;
using SIGH.Domain.Disciplinary.Entities;

namespace SIGH.Persistence.Configurations.Disciplinary;

public class DisciplinaryEvidenceConfiguration : IEntityTypeConfiguration<DisciplinaryEvidence>
{
    public void Configure(EntityTypeBuilder<DisciplinaryEvidence> builder)
    {
        builder.ToTable("DisciplinaryEvidences");

        builder.HasKey(ev => ev.Id);

        builder.Property(ev => ev.DisciplinaryCaseId)
            .IsRequired();

        builder.Property(ev => ev.EvidenceType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(ev => ev.Title)
            .IsRequired()
            .HasMaxLength(DisciplinaryDomainConstants.EvidenceTitleMaxLength);

        builder.Property(ev => ev.Description)
            .HasMaxLength(DisciplinaryDomainConstants.DescriptionMaxLength);

        builder.Property(ev => ev.StorageReference)
            .HasMaxLength(DisciplinaryDomainConstants.StorageReferenceMaxLength);

        builder.Property(ev => ev.OriginalFileName)
            .HasMaxLength(DisciplinaryDomainConstants.OriginalFileNameMaxLength);

        builder.Property(ev => ev.ContentType)
            .HasMaxLength(DisciplinaryDomainConstants.ContentTypeMaxLength);

        builder.Property(ev => ev.IntegrityHash)
            .HasMaxLength(DisciplinaryDomainConstants.IntegrityHashMaxLength);

        builder.Property(ev => ev.CollectedAt)
            .IsRequired();

        builder.Property(ev => ev.CollectedByUserId)
            .IsRequired();

        builder.Property(ev => ev.Status)
            .IsRequired()
            .HasConversion<int>();

        // Relationship
        builder.HasOne<DisciplinaryOccurrence>()
            .WithMany()
            .HasForeignKey(ev => ev.DisciplinaryOccurrenceId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(ev => ev.DisciplinaryCaseId);
        builder.HasIndex(ev => ev.DisciplinaryOccurrenceId);
        builder.HasIndex(ev => ev.EvidenceType);
        builder.HasIndex(ev => ev.Status);
        builder.HasIndex(ev => ev.CollectedAt);
        builder.HasIndex(ev => ev.IsDeleted);

        // Composite Index
        builder.HasIndex(ev => new { ev.DisciplinaryCaseId, ev.Status });
    }
}
