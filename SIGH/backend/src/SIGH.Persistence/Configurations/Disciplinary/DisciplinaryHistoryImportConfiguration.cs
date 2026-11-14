using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGH.Domain.Disciplinary.Entities;

namespace SIGH.Persistence.Configurations.Disciplinary;

public class DisciplinaryHistoryImportBatchConfiguration : IEntityTypeConfiguration<DisciplinaryHistoryImportBatch>
{
    public void Configure(EntityTypeBuilder<DisciplinaryHistoryImportBatch> builder)
    {
        builder.ToTable("DisciplinaryHistoryImportBatches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.FileName).HasMaxLength(DisciplinaryHistoryImportBatch.MaxFileNameLength).IsRequired();
        builder.Property(b => b.FileSha256).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(b => b.PlanFingerprint).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.HasIndex(b => b.PreviewedAt);
        builder.HasIndex(b => b.FileSha256);
    }
}

public class HistoryImportStateConfiguration : AuditableEntityConfiguration<HistoryImportState>
{
    public override void Configure(EntityTypeBuilder<HistoryImportState> builder)
    {
        base.Configure(builder);
        builder.ToTable("HistoryImportStates");
    }
}
