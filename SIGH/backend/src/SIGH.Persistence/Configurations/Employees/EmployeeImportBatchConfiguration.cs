using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SIGH.Persistence.Configurations.Employees;

public class EmployeeImportBatchConfiguration : IEntityTypeConfiguration<EmployeeImportBatch>
{
    public void Configure(EntityTypeBuilder<EmployeeImportBatch> builder)
    {
        builder.ToTable("EmployeeImportBatches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.FileName).HasMaxLength(EmployeeImportBatch.MaxFileNameLength).IsRequired();
        builder.Property(b => b.FileSha256).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(b => b.Status).HasConversion<int>().IsRequired();
        builder.Property(b => b.PlanFingerprint).HasMaxLength(64).IsRequired();
        builder.HasIndex(b => b.PreviewedAt);
        builder.HasIndex(b => b.FileSha256);
    }
}
