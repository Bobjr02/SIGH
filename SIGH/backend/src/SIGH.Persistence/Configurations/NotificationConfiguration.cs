namespace SIGH.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGH.Domain.Notifications.Entities;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.CompanyId)
            .IsRequired();

        builder.Property(n => n.UserId)
            .IsRequired();

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(n => n.Type)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Info");

        builder.Property(n => n.Priority)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Medium");

        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.IsExpired)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.Metadata)
            .HasMaxLength(4000);

        builder.HasIndex(n => new { n.CompanyId, n.UserId, n.IsRead });
        builder.HasIndex(n => new { n.CompanyId, n.UserId, n.CreatedAt });
    }
}
