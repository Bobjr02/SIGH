using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SIGH.Persistence.Configurations.Employees;

public class DepartmentConfiguration : AuditableEntityConfiguration<Department>
{
    public override void Configure(EntityTypeBuilder<Department> builder)
    {
        base.Configure(builder);

        builder.ToTable("Departments");

        builder.Property(d => d.CompanyId)
            .IsRequired();

        builder.Property(d => d.ManagementUnitId)
            .IsRequired();

        builder.Property(d => d.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(d => d.Code)
            .HasMaxLength(30);

        builder.Property(d => d.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(d => d.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ManagementUnit>()
            .WithMany()
            .HasForeignKey(d => d.ManagementUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.CompanyId)
            .HasDatabaseName("IX_Departments_CompanyId");

        builder.HasIndex(d => d.ManagementUnitId)
            .HasDatabaseName("IX_Departments_ManagementUnitId");

        builder.HasIndex(d => new { d.CompanyId, d.Name })
            .HasDatabaseName("IX_Departments_CompanyId_Name");

        builder.HasIndex(d => new { d.ManagementUnitId, d.Name })
            .HasDatabaseName("IX_Departments_ManagementUnitId_Name");

        builder.HasIndex(d => d.Code)
            .HasDatabaseName("IX_Departments_Code");
    }
}
