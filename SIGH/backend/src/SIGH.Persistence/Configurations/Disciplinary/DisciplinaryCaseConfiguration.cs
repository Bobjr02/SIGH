using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGH.Domain.Disciplinary.Constants;
using SIGH.Domain.Disciplinary.Entities;

namespace SIGH.Persistence.Configurations.Disciplinary;

public class DisciplinaryCaseConfiguration : IEntityTypeConfiguration<DisciplinaryCase>
{
    public void Configure(EntityTypeBuilder<DisciplinaryCase> builder)
    {
        builder.ToTable("DisciplinaryCases");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CaseNumber)
            .IsRequired()
            .HasMaxLength(DisciplinaryDomainConstants.CaseNumberMaxLength);

        builder.Property(c => c.CompanyId)
            .IsRequired();

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(DisciplinaryDomainConstants.TitleMaxLength);

        builder.Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(DisciplinaryDomainConstants.DescriptionMaxLength);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.Priority)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.OpenedAt)
            .IsRequired();

        builder.Property(c => c.OpenedByUserId)
            .IsRequired();

        builder.Property(c => c.CancellationReason)
            .HasMaxLength(DisciplinaryDomainConstants.CancellationReasonMaxLength);

        builder.Property(c => c.ConclusionSummary)
            .HasMaxLength(DisciplinaryDomainConstants.ConclusionSummaryMaxLength);

        // Calculated properties ignored
        builder.Ignore(c => c.Decision);
        builder.Ignore(c => c.DecisionId);

        // Backing field collection mappings
        builder.HasMany(c => c.Occurrences)
            .WithOne()
            .HasForeignKey(o => o.DisciplinaryCaseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(c => c.Occurrences)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(c => c.Employees)
            .WithOne()
            .HasForeignKey(e => e.DisciplinaryCaseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(c => c.Employees)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(c => c.Evidences)
            .WithOne()
            .HasForeignKey(ev => ev.DisciplinaryCaseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(c => c.Evidences)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(c => c.Decisions)
            .WithOne()
            .HasForeignKey(d => d.DisciplinaryCaseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(c => c.Decisions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(c => c.Measures)
            .WithOne()
            .HasForeignKey(m => m.DisciplinaryCaseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(c => c.Measures)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(c => c.CompanyId);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.Priority);
        builder.HasIndex(c => c.ResponsibleEmployeeId);
        builder.HasIndex(c => c.OpenedAt);
        builder.HasIndex(c => c.DueDate);
        builder.HasIndex(c => c.IsDeleted);

        // Composite Indexes
        builder.HasIndex(c => new { c.CompanyId, c.Status });
        builder.HasIndex(c => new { c.CompanyId, c.OpenedAt });
        builder.HasIndex(c => new { c.CompanyId, c.IsDeleted });

        // Filtered Unique Index
        builder.HasIndex(c => new { c.CompanyId, c.CaseNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_DisciplinaryCases_Company_CaseNumber");
    }
}
