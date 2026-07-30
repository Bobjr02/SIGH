using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SIGH.Persistence.Configurations.Employees;

public class JobTitleConfiguration : AuditableEntityConfiguration<JobTitle>
{
    public override void Configure(EntityTypeBuilder<JobTitle> builder)
    {
        base.Configure(builder);

        builder.ToTable("JobTitles");

        builder.Property(j => j.CompanyId)
            .IsRequired();

        builder.Property(j => j.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(j => j.Code)
            .HasMaxLength(30);

        builder.Property(j => j.Description)
            .HasMaxLength(500);

        builder.Property(j => j.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(j => j.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(j => j.CompanyId)
            .HasDatabaseName("IX_JobTitles_CompanyId");

        builder.HasIndex(j => new { j.CompanyId, j.Name })
            .HasDatabaseName("IX_JobTitles_CompanyId_Name");

        builder.HasIndex(j => new { j.CompanyId, j.Code })
            .HasDatabaseName("IX_JobTitles_CompanyId_Code");
    }
}
