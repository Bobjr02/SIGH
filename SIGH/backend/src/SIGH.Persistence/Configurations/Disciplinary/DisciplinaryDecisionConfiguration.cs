using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGH.Domain.Disciplinary.Constants;
using SIGH.Domain.Disciplinary.Entities;

namespace SIGH.Persistence.Configurations.Disciplinary;

public class DisciplinaryDecisionConfiguration : IEntityTypeConfiguration<DisciplinaryDecision>
{
    public void Configure(EntityTypeBuilder<DisciplinaryDecision> builder)
    {
        builder.ToTable("DisciplinaryDecisions");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.DisciplinaryCaseId)
            .IsRequired();

        builder.Property(d => d.DecisionType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(d => d.Summary)
            .IsRequired()
            .HasMaxLength(DisciplinaryDomainConstants.SummaryMaxLength);

        builder.Property(d => d.Reasoning)
            .IsRequired()
            .HasMaxLength(DisciplinaryDomainConstants.ReasoningMaxLength);

        builder.Property(d => d.DecidedAt)
            .IsRequired();

        builder.Property(d => d.DecidedByUserId)
            .IsRequired();

        builder.Property(d => d.Status)
            .IsRequired()
            .HasConversion<int>();

        // Indexes
        builder.HasIndex(d => d.DisciplinaryCaseId);
        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => d.DecisionType);
        builder.HasIndex(d => d.DecidedAt);
        builder.HasIndex(d => d.ApprovedAt);
        builder.HasIndex(d => d.IsDeleted);

        // Composite Index
        builder.HasIndex(d => new { d.DisciplinaryCaseId, d.Status });
    }
}
