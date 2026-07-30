using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGH.Domain.Disciplinary.Constants;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Employees.Entities;

namespace SIGH.Persistence.Configurations.Disciplinary;

public class DisciplinaryCaseEmployeeConfiguration : IEntityTypeConfiguration<DisciplinaryCaseEmployee>
{
    public void Configure(EntityTypeBuilder<DisciplinaryCaseEmployee> builder)
    {
        builder.ToTable("DisciplinaryCaseEmployees");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.DisciplinaryCaseId)
            .IsRequired();

        builder.Property(e => e.EmployeeId)
            .IsRequired();

        builder.Property(e => e.Role)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.IsPrimarySubject)
            .IsRequired();

        builder.Property(e => e.Statement)
            .HasMaxLength(DisciplinaryDomainConstants.DescriptionMaxLength);

        // Relationship
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.DisciplinaryCaseId);
        builder.HasIndex(e => e.EmployeeId);
        builder.HasIndex(e => e.Role);
        builder.HasIndex(e => e.IsPrimarySubject);
        builder.HasIndex(e => e.IsDeleted);

        // Filtered Unique Index
        builder.HasIndex(e => new { e.DisciplinaryCaseId, e.EmployeeId, e.Role })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_DisciplinaryCaseEmployees_Case_Employee_Role");
    }
}
