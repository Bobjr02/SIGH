using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SIGH.Persistence.Configurations.Employees;

public class EmployeeConfiguration : AuditableEntityConfiguration<Employee>
{
    public override void Configure(EntityTypeBuilder<Employee> builder)
    {
        base.Configure(builder);

        builder.ToTable("Employees");

        builder.Property(e => e.CompanyId)
            .IsRequired();

        builder.Property(e => e.EmployeeNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.FullName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(e => e.SocialName)
            .HasMaxLength(150);

        builder.Property(e => e.Cpf)
            .HasMaxLength(11)
            .IsRequired();

        builder.Property(e => e.AdmissionDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(e => e.BirthDate)
            .HasColumnType("date");

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.JobTitleId)
            .IsRequired();

        builder.Property(e => e.ManagementUnitId)
            .IsRequired();

        builder.Property(e => e.CorporateEmail)
            .HasMaxLength(150);

        builder.Property(e => e.PersonalEmail)
            .HasMaxLength(150);

        builder.Property(e => e.MobileNumber)
            .HasMaxLength(20);

        builder.Property(e => e.TerminationDate)
            .HasColumnType("date");

        builder.Property(e => e.TerminationReason)
            .HasMaxLength(500);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        // Propriedade computada do domínio - não mapeada para o banco de dados
        builder.Ignore(e => e.HasSystemAccess);

        // Relacionamentos
        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<JobTitle>()
            .WithMany()
            .HasForeignKey(e => e.JobTitleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ManagementUnit>()
            .WithMany()
            .HasForeignKey(e => e.ManagementUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(e => e.SupervisorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices de Unicidade
        builder.HasIndex(e => e.Cpf)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Employees_Cpf");

        builder.HasIndex(e => new { e.CompanyId, e.EmployeeNumber })
            .IsUnique()
            .HasDatabaseName("IX_Employees_CompanyId_EmployeeNumber");

        builder.HasIndex(e => e.UserId)
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL")
            .HasDatabaseName("IX_Employees_UserId");

        builder.HasIndex(e => e.CorporateEmail)
            .IsUnique()
            .HasFilter("[CorporateEmail] IS NOT NULL")
            .HasDatabaseName("IX_Employees_CorporateEmail");

        // Índices adicionais de consulta e filtragem
        builder.HasIndex(e => e.FullName)
            .HasDatabaseName("IX_Employees_FullName");

        builder.HasIndex(e => e.SocialName)
            .HasDatabaseName("IX_Employees_SocialName");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_Employees_Status");

        builder.HasIndex(e => e.CompanyId)
            .HasDatabaseName("IX_Employees_CompanyId");

        builder.HasIndex(e => e.ManagementUnitId)
            .HasDatabaseName("IX_Employees_ManagementUnitId");

        builder.HasIndex(e => e.DepartmentId)
            .HasDatabaseName("IX_Employees_DepartmentId");

        builder.HasIndex(e => e.JobTitleId)
            .HasDatabaseName("IX_Employees_JobTitleId");

        builder.HasIndex(e => e.SupervisorId)
            .HasDatabaseName("IX_Employees_SupervisorId");

        builder.HasIndex(e => e.AdmissionDate)
            .HasDatabaseName("IX_Employees_AdmissionDate");

        builder.HasIndex(e => new { e.CompanyId, e.Status })
            .HasDatabaseName("IX_Employees_CompanyId_Status");

        builder.HasIndex(e => new { e.ManagementUnitId, e.Status })
            .HasDatabaseName("IX_Employees_ManagementUnitId_Status");

        builder.HasIndex(e => new { e.CompanyId, e.FullName })
            .HasDatabaseName("IX_Employees_CompanyId_FullName");
    }
}
