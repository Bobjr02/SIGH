using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SIGH.Persistence.Configurations.Employees;

public class CompanyConfiguration : AuditableEntityConfiguration<Company>
{
    public override void Configure(EntityTypeBuilder<Company> builder)
    {
        base.Configure(builder);

        builder.ToTable("Companies");

        builder.Property(c => c.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.LegalName)
            .HasMaxLength(200);

        builder.Property(c => c.RegistrationNumber)
            .HasMaxLength(20);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(c => c.Name)
            .HasDatabaseName("IX_Companies_Name");

        builder.HasIndex(c => c.RegistrationNumber)
            .HasDatabaseName("IX_Companies_RegistrationNumber");
    }
}
