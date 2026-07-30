using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SIGH.Persistence.Configurations;

public class RefreshTokenConfiguration : AuditableEntityConfiguration<RefreshToken>
{
    public override void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        base.Configure(builder);

        builder.ToTable("RefreshTokens");

        builder.Property(rt => rt.TokenHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(rt => rt.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_TokenHash");

        builder.Property(rt => rt.ReplacedByTokenHash)
            .HasMaxLength(256);

        builder.Property(rt => rt.ReasonRevoked)
            .HasMaxLength(250);

        builder.Property(rt => rt.IpAddress)
            .HasMaxLength(45);

        builder.Property(rt => rt.UserAgent)
            .HasMaxLength(500);

        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
