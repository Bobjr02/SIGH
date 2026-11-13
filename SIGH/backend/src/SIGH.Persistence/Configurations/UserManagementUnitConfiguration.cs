using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SIGH.Persistence.Configurations;

public class UserManagementUnitConfiguration : AuditableEntityConfiguration<UserManagementUnit>
{
    public override void Configure(EntityTypeBuilder<UserManagementUnit> builder)
    {
        base.Configure(builder);

        builder.ToTable("UserManagementUnits");

        // Um vínculo ATIVO por gerência; o retirado (exclusão lógica) fica na história.
        builder.HasIndex(x => new { x.UserId, x.ManagementUnitId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_UserManagementUnits_UserId_ManagementUnitId");

        builder.HasIndex(x => x.ManagementUnitId);

        builder.HasOne(x => x.User)
            .WithMany(u => u.ManagementUnits)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ManagementUnit)
            .WithMany()
            .HasForeignKey(x => x.ManagementUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
