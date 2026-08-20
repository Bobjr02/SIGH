using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGH.Domain.Disciplinary.Constants;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Employees.Entities;

namespace SIGH.Persistence.Configurations.Disciplinary;

public class DisciplinaryMeasureConfiguration : IEntityTypeConfiguration<DisciplinaryMeasure>
{
    public void Configure(EntityTypeBuilder<DisciplinaryMeasure> builder)
    {
        builder.ToTable("DisciplinaryMeasures");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.DisciplinaryCaseId)
            .IsRequired();

        builder.Property(m => m.DisciplinaryDecisionId)
            .IsRequired();

        builder.Property(m => m.EmployeeId)
            .IsRequired();

        builder.Property(m => m.MeasureType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.Reason)
            .IsRequired()
            .HasMaxLength(DisciplinaryDomainConstants.ReasonMaxLength);

        builder.Property(m => m.EffectiveFrom)
            .IsRequired();

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.Notes)
            .HasMaxLength(DisciplinaryDomainConstants.NotesMaxLength);

        // Relationships
        builder.HasOne<DisciplinaryDecision>()
            .WithMany()
            .HasForeignKey(m => m.DisciplinaryDecisionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(m => m.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(m => m.DisciplinaryCaseId);
        builder.HasIndex(m => m.DisciplinaryDecisionId);
        builder.HasIndex(m => m.EmployeeId);
        builder.HasIndex(m => m.MeasureType);
        builder.HasIndex(m => m.Status);
        builder.HasIndex(m => m.AppliedAt);
        builder.HasIndex(m => m.IsDeleted);

        // Composite Indexes
        builder.HasIndex(m => new { m.DisciplinaryCaseId, m.Status });
        builder.HasIndex(m => new { m.DisciplinaryDecisionId, m.Status });
    }
}
