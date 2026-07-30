using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SIGH.Persistence.Configurations.Employees;

public class ManagementUnitConfiguration : AuditableEntityConfiguration<ManagementUnit>
{
    public override void Configure(EntityTypeBuilder<ManagementUnit> builder)
    {
        base.Configure(builder);

        builder.ToTable("ManagementUnits");

        builder.Property(m => m.CompanyId)
            .IsRequired();

        builder.Property(m => m.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(m => m.Code)
            .HasMaxLength(30);

        builder.Property(m => m.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(m => m.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ManagementUnit>()
            .WithMany()
            .HasForeignKey(m => m.ParentManagementUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.CompanyId)
            .HasDatabaseName("IX_ManagementUnits_CompanyId");

        builder.HasIndex(m => m.ParentManagementUnitId)
            .HasDatabaseName("IX_ManagementUnits_ParentManagementUnitId");

        builder.HasIndex(m => new { m.CompanyId, m.Name })
            .HasDatabaseName("IX_ManagementUnits_CompanyId_Name");

        builder.HasIndex(m => new { m.CompanyId, m.Code })
            .HasDatabaseName("IX_ManagementUnits_CompanyId_Code");
    }
}
