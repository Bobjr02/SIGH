using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SIGH.Persistence.Configurations;

public class AuditLogConfiguration : AuditableEntityConfiguration<AuditLog>
{
    public override void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        base.Configure(builder);

        builder.ToTable("AuditLogs");

        builder.Property(al => al.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(al => al.Entity)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(al => al.EntityId)
            .HasMaxLength(100);

        builder.Property(al => al.IpAddress)
            .HasMaxLength(45);

        builder.Property(al => al.UserAgent)
            .HasMaxLength(500);

        builder.Property(al => al.CorrelationId)
            .HasMaxLength(100);

        // Consultas da trilha: por entidade (linha do tempo de um processo) e por período.
        builder.HasIndex(al => new { al.Entity, al.EntityId });
        builder.HasIndex(al => al.CreatedAt);
        builder.HasIndex(al => al.UserId);

        builder.HasOne(al => al.User)
            .WithMany()
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
