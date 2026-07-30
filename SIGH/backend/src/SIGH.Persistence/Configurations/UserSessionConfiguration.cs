using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SIGH.Persistence.Configurations;

public class UserSessionConfiguration : AuditableEntityConfiguration<UserSession>
{
    public override void Configure(EntityTypeBuilder<UserSession> builder)
    {
        base.Configure(builder);

        builder.ToTable("UserSessions");

        builder.Property(us => us.DeviceName)
            .HasMaxLength(100);

        builder.Property(us => us.Browser)
            .HasMaxLength(100);

        builder.Property(us => us.OperatingSystem)
            .HasMaxLength(100);

        builder.Property(us => us.IpAddress)
            .HasMaxLength(45);

        builder.HasOne(us => us.User)
            .WithMany(u => u.Sessions)
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
