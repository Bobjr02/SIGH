using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGH.Domain.Disciplinary.Constants;
using SIGH.Domain.Disciplinary.Entities;

namespace SIGH.Persistence.Configurations.Disciplinary;

public class InfractionTypeConfiguration : IEntityTypeConfiguration<InfractionType>
{
    public void Configure(EntityTypeBuilder<InfractionType> builder)
    {
        builder.ToTable("InfractionTypes");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Code)
            .IsRequired()
            .HasMaxLength(DisciplinaryDomainConstants.InfractionCodeMaxLength);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(DisciplinaryDomainConstants.InfractionNameMaxLength);

        builder.Property(i => i.Description)
            .HasMaxLength(DisciplinaryDomainConstants.DescriptionMaxLength);

        builder.Property(i => i.DefaultSeverity)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(i => i.IsActive)
            .IsRequired();

        builder.Property(i => i.RequiresFormalInvestigation)
            .IsRequired();

        builder.Property(i => i.AllowsTerminationRecommendation)
            .IsRequired();

        builder.Property(i => i.LegalReference)
            .HasMaxLength(DisciplinaryDomainConstants.LegalReferenceMaxLength);

        // Indexes
        builder.HasIndex(i => i.Name);
        builder.HasIndex(i => i.DefaultSeverity);
        builder.HasIndex(i => i.IsActive);
        builder.HasIndex(i => i.IsDeleted);

        // Filtered Unique Index
        builder.HasIndex(i => i.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_InfractionTypes_Code");
    }
}
