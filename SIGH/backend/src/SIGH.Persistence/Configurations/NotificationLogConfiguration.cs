namespace SIGH.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGH.Domain.Notifications.Entities;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("NotificationLogs");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.CompanyId)
            .IsRequired();

        builder.Property(n => n.SourceEntity)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(n => n.SourceEntityId)
            .IsRequired();

        builder.Property(n => n.EventType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(n => n.RecipientUserId)
            .IsRequired();

        builder.Property(n => n.LastSentAt)
            .IsRequired();

        builder.Property(n => n.RemindersSentCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(n => n.EscalationLevel)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(n => n.Metadata)
            .HasMaxLength(4000);

        builder.HasIndex(n => new { n.CompanyId, n.SourceEntity, n.SourceEntityId, n.RecipientUserId, n.EventType });
        builder.HasIndex(n => new { n.CompanyId, n.LastSentAt });
    }
}
