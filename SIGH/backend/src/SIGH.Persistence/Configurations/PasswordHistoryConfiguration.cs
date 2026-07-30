using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SIGH.Persistence.Configurations;

public class PasswordHistoryConfiguration : AuditableEntityConfiguration<PasswordHistory>
{
    public override void Configure(EntityTypeBuilder<PasswordHistory> builder)
    {
        base.Configure(builder);

        builder.ToTable("PasswordHistories");

        builder.Property(ph => ph.PasswordHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasOne(ph => ph.User)
            .WithMany(u => u.PasswordHistories)
            .HasForeignKey(ph => ph.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
